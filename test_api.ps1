$BASE = "http://majko.ddns.net:9000"
$pass = 0
$fail = 0
$token = $null

function Test($label, $method, $url, $body = $null, $useAuth = $true) {
    $params = @{ Method = $method; Uri = "$BASE$url"; ErrorAction = "Stop" }
    if ($body) {
        $params.Body        = ($body | ConvertTo-Json)
        $params.ContentType = "application/json"
    }
    if ($useAuth -and $script:token) {
        $params.Headers = @{ Authorization = "Bearer $($script:token)" }
    }
    try {
        $r = Invoke-RestMethod @params
        Write-Host "  [OK] $label" -ForegroundColor Green
        $script:pass++
        return $r
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        Write-Host "  [FAIL] $label  ($code $($_.Exception.Message))" -ForegroundColor Red
        $script:fail++
        return $null
    }
}

# ── Auth ──────────────────────────────────────
Write-Host "`n=== AUTH ===" -ForegroundColor Cyan

$reg = Test "Rejestracja" POST "/auth/register" @{ username = "testuser"; password = "haslo123" } -useAuth $false
$script:token = $reg.token
Write-Host "  Token: $($script:token.Substring(0, [Math]::Min(40, $script:token.Length)))..." -ForegroundColor DarkGray

$login = Test "Logowanie" POST "/auth/login" @{ username = "testuser"; password = "haslo123" } -useAuth $false
$script:token = $login.token

Test "Blad logowania (zle haslo)" POST "/auth/login" @{ username = "testuser"; password = "zlehaslo" } -useAuth $false
Test "Profil zalogowanego (me)" GET "/auth/me"

# ── Klienci ──────────────────────────────────
Write-Host "`n=== KLIENCI ===" -ForegroundColor Cyan

$c1 = Test "Utworz klienta 1 (Jan Kowalski)"  POST "/bank/clients?firstName=Jan&lastName=Kowalski"
$c2 = Test "Utworz klienta 2 (Anna Nowak)"    POST "/bank/clients?firstName=Anna&lastName=Nowak"

$id1 = $c1.accountId
$id2 = $c2.accountId

Test "Pobierz wszystkich klientow"  GET "/bank/clients"
Test "Pobierz klienta 1"            GET "/bank/clients/$id1"
Test "Aktualizuj klienta 1"         PUT "/bank/clients/$id1" @{ firstName = "Janek"; lastName = "Kowalski" }
Test "Pobierz klienta 1 po update"  GET "/bank/clients/$id1"

# ── Link konta bankowego do usera ──────────────
Write-Host "`n=== LINK USER-CLIENT ===" -ForegroundColor Cyan

Test "Przypnij konto 1 do zalogowanego usera"  POST "/auth/link-client/$id1"
Test "Profil po przypianiu (me)"               GET  "/auth/me"

# ── Karty ─────────────────────────────────────
Write-Host "`n=== KARTY ===" -ForegroundColor Cyan

$card1 = Test "Utworz karte dla klienta 1"  POST "/bank/clients/$id1/cards"
$card2 = Test "Utworz karte dla klienta 2"  POST "/bank/clients/$id2/cards"

$cn1 = $card1.cardNumber
$cn2 = $card2.cardNumber

Test "Pobierz karte 1"              GET "/bank/cards/$cn1"
Test "Pobierz karty klienta 1"      GET "/bank/clients/$id1/cards"
Test "Ustaw karte glowna klienta 1" PUT "/bank/clients/$id1/primary-card/$cn1"

# ── Saldo ─────────────────────────────────────
Write-Host "`n=== SALDO ===" -ForegroundColor Cyan

Test "Saldo karty 1 (oczekiwane: 0)"    GET "/bank/cards/$cn1/balance"
Test "Laczne saldo klienta 1"           GET "/bank/clients/$id1/balance"

# ── Transakcje ────────────────────────────────
Write-Host "`n=== TRANSAKCJE ===" -ForegroundColor Cyan

Test "Receive: zasil konto 1 kwota 100" POST "/transaction/receive" @{
    fromAccountId = "0000000000000000000000000000"
    toAccountId   = $id1
    amount        = 100
}

Test "Saldo karty 1 po zasileniu (100)"  GET "/bank/cards/$cn1/balance"

Test "Send: przelew wewnetrzny 1->2 (30)" POST "/transaction/send" @{
    fromAccountId = $id1
    toAccountId   = $id2
    amount        = 30
}

Test "Saldo karty 1 po przelewie (70)"  GET "/bank/cards/$cn1/balance"
Test "Saldo karty 2 po przelewie (30)"  GET "/bank/cards/$cn2/balance"

Test "Historia transakcji klienta 1"    GET "/transaction/account/$id1"
Test "Pobierz transakcje nr 1"          GET "/transaction/1"

# ── Brak tokenu -> 401 ────────────────────────
Write-Host "`n=== AUTORYZACJA ===" -ForegroundColor Cyan

$savedToken = $script:token
$script:token = $null
$params401 = @{ Method = "GET"; Uri = "$BASE/bank/clients"; ErrorAction = "Stop" }
try {
    Invoke-RestMethod @params401 | Out-Null
    Write-Host "  [FAIL] Brak tokenu powinien zwrocic 401" -ForegroundColor Red
    $fail++
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 401) {
        Write-Host "  [OK] Brak tokenu zwrocil 401 - poprawnie" -ForegroundColor Green
        $pass++
    } else {
        Write-Host "  [FAIL] Oczekiwano 401, dostano $code" -ForegroundColor Red
        $fail++
    }
}
$script:token = $savedToken

# ── Usuwanie ──────────────────────────────────
Write-Host "`n=== USUWANIE ===" -ForegroundColor Cyan

Test "Usun karte 1"    DELETE "/bank/cards/$cn1"
Test "Usun klienta 2"  DELETE "/bank/clients/$id2"

$params404 = @{ Method = "GET"; Uri = "$BASE/bank/clients/$id2"; ErrorAction = "Stop"; Headers = @{ Authorization = "Bearer $($script:token)" } }
try {
    Invoke-RestMethod @params404 | Out-Null
    Write-Host "  [FAIL] Pobierz usunietego klienta (oczekiwano 404, dostano 200)" -ForegroundColor Red
    $fail++
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 404) {
        Write-Host "  [OK] Pobierz usunietego klienta (404 - poprawnie)" -ForegroundColor Green
        $pass++
    } else {
        Write-Host "  [FAIL] Oczekiwano 404, dostano $code" -ForegroundColor Red
        $fail++
    }
}

# ── Wynik ─────────────────────────────────────
Write-Host "`n---------------------------------" -ForegroundColor DarkGray
$color = if ($fail -eq 0) { "Green" } else { "Yellow" }
Write-Host "Wynik: $pass OK, $fail FAIL" -ForegroundColor $color
