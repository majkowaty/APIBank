$BASE  = "http://majko.ddns.net:9000"
$pass  = 0
$fail  = 0
$token = $null

# Unikalny username zeby nie kolidowac z poprzednimi testami
$testUser = "testuser_$(Get-Random -Maximum 9999)"

function Test($label, $method, $url, $body = $null, $useAuth = $true, $expectCode = $null) {
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
        if ($expectCode -and $expectCode -ne 200) {
            Write-Host "  [FAIL] $label  (oczekiwano $expectCode, dostano 200)" -ForegroundColor Red
            $script:fail++
            return $null
        }
        Write-Host "  [OK] $label" -ForegroundColor Green
        $script:pass++
        return $r
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        if ($expectCode -and $code -eq $expectCode) {
            Write-Host "  [OK] $label  ($code jak oczekiwano)" -ForegroundColor Green
            $script:pass++
            return $null
        }
        Write-Host "  [FAIL] $label  ($code $($_.Exception.Message))" -ForegroundColor Red
        $script:fail++
        return $null
    }
}

# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n=== 1. BRAK TOKENU ===" -ForegroundColor Cyan
Test "GET /bank/clients bez tokenu -> 401"         GET  "/bank/clients"            -useAuth $false -expectCode 401
Test "GET /transaction/account/xxx bez tokenu -> 401" GET "/transaction/account/xxx" -useAuth $false -expectCode 401
Test "GET /auth/me bez tokenu -> 401"              GET  "/auth/me"                 -useAuth $false -expectCode 401

# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n=== 2. REJESTRACJA I LOGOWANIE ===" -ForegroundColor Cyan
$reg = Test "Rejestracja nowego usera" POST "/auth/register" @{ username = $testUser; password = "haslo123" } -useAuth $false
$script:token = $reg.token
Write-Host "    user: $($reg.username)  token: $($script:token.Substring(0,40))..." -ForegroundColor DarkGray

Test "Duplikat nazwy usera -> blad" POST "/auth/register" @{ username = $testUser; password = "abc" } -useAuth $false -expectCode 400

$login = Test "Logowanie poprawne"                 POST "/auth/login" @{ username = $testUser; password = "haslo123" } -useAuth $false
$script:token = $login.token

Test "Logowanie zle haslo -> 401"                  POST "/auth/login" @{ username = $testUser; password = "zle" } -useAuth $false -expectCode 401

Test "GET /auth/me (zalogowany)"                   GET  "/auth/me"

# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n=== 3. KLIENCI ===" -ForegroundColor Cyan
$c1 = Test "Utworz klienta 1 (Jan Kowalski)"  POST "/bank/clients?firstName=Jan&lastName=Kowalski"
$c2 = Test "Utworz klienta 2 (Anna Nowak)"    POST "/bank/clients?firstName=Anna&lastName=Nowak"
$id1 = $c1.accountId
$id2 = $c2.accountId

Test "Pobierz wszystkich klientow"      GET  "/bank/clients"
Test "Pobierz klienta 1"               GET  "/bank/clients/$id1"
Test "Aktualizuj klienta 1"            PUT  "/bank/clients/$id1" @{ firstName = "Janek"; lastName = "Kowalski" }
$updated = Test "Pobierz klienta 1 po update" GET "/bank/clients/$id1"
if ($updated -and $updated.firstName -eq "Janek") {
    Write-Host "    firstName poprawnie zmieniony na 'Janek'" -ForegroundColor DarkGray
} else {
    Write-Host "    [WARN] firstName nie zostal zmieniony poprawnie" -ForegroundColor Yellow
}
Test "Pobierz nieistniejacego klienta -> 404" GET "/bank/clients/0000000000000000000000000000" -expectCode 404

# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n=== 4. LINK USER -> CLIENT ===" -ForegroundColor Cyan
Test "Przypnij konto 1 do zalogowanego usera"   POST "/auth/link-client/$id1"
$me = Test "GET /auth/me po przypianiu"         GET  "/auth/me"
if ($me -and $me.clientAccountId -eq $id1) {
    Write-Host "    clientAccountId poprawnie ustawiony na $id1" -ForegroundColor DarkGray
} else {
    Write-Host "    [WARN] clientAccountId nie zgadza sie: $($me.clientAccountId)" -ForegroundColor Yellow
}

# Drugi user probuje przypisac to samo konto
$reg2 = Test "Rejestracja drugiego usera"  POST "/auth/register" @{ username = "${testUser}_2"; password = "haslo123" } -useAuth $false
$token2 = $reg2.token
$savedToken = $script:token
$script:token = $token2
Test "Drugi user przypina cudze konto -> blad"  POST "/auth/link-client/$id1" -expectCode 400
$script:token = $savedToken

# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n=== 5. KARTY ===" -ForegroundColor Cyan
$card1 = Test "Utworz karte dla klienta 1"  POST "/bank/clients/$id1/cards"
$card2 = Test "Utworz karte dla klienta 2"  POST "/bank/clients/$id2/cards"
$cn1   = $card1.cardNumber
$cn2   = $card2.cardNumber

Test "Pobierz karte 1"               GET "/bank/cards/$cn1"
Test "Pobierz nieistniejaca karte -> 404" GET "/bank/cards/0000000000000" -expectCode 404
Test "Pobierz karty klienta 1"       GET "/bank/clients/$id1/cards"
Test "Ustaw karte glowna klienta 1"  PUT "/bank/clients/$id1/primary-card/$cn1"
$c1After = Test "Klient 1 ma karte glowna"  GET "/bank/clients/$id1"
if ($c1After -and $c1After.primaryCardNumber -eq $cn1) {
    Write-Host "    primaryCardNumber poprawnie ustawiony" -ForegroundColor DarkGray
}

# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n=== 6. SALDO ===" -ForegroundColor Cyan
$bal = Test "Saldo karty 1 (oczekiwane: 0)"  GET "/bank/cards/$cn1/balance"
if ($bal -and $bal.balance -eq 0) { Write-Host "    balance = 0 (poprawnie)" -ForegroundColor DarkGray }
Test "Laczne saldo klienta 1"                GET "/bank/clients/$id1/balance"

# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n=== 7. TRANSAKCJE ===" -ForegroundColor Cyan

Test "Receive: zasil konto 1 kwota 100"  POST "/transaction/receive" @{
    fromAccountId = "0000000000000000000000000000"; toAccountId = $id1; amount = 100 }

$b1 = Test "Saldo karty 1 po zasileniu (oczekiwane 100)"  GET "/bank/cards/$cn1/balance"
if ($b1 -and $b1.balance -eq 100) { Write-Host "    balance = 100 (poprawnie)" -ForegroundColor DarkGray }

Test "Send: przelew wewnetrzny 1->2 kwota 30"  POST "/transaction/send" @{
    fromAccountId = $id1; toAccountId = $id2; amount = 30 }

$b1after = Test "Saldo karty 1 po przelewie (oczekiwane 70)"  GET "/bank/cards/$cn1/balance"
if ($b1after -and $b1after.balance -eq 70) { Write-Host "    balance = 70 (poprawnie)" -ForegroundColor DarkGray }

$b2after = Test "Saldo karty 2 po przelewie (oczekiwane 30)"  GET "/bank/cards/$cn2/balance"
if ($b2after -and $b2after.balance -eq 30) { Write-Host "    balance = 30 (poprawnie)" -ForegroundColor DarkGray }

# Send z cudzego konta -> 403
Test "Send z cudzego konta -> 403 Forbidden"  POST "/transaction/send" @{
    fromAccountId = $id2; toAccountId = $id1; amount = 10 } -expectCode 403

$txList = Test "Historia transakcji klienta 1"  GET "/transaction/account/$id1"
if ($txList) { Write-Host "    liczba transakcji: $($txList.Count)" -ForegroundColor DarkGray }

$txId = if ($txList -and $txList.Count -gt 0) { $txList[0].transactionId } else { 1 }
Test "Pobierz transakcje nr $txId"       GET "/transaction/$txId"
Test "Pobierz nieistniejaca transakcje -> 404"  GET "/transaction/999999" -expectCode 404

# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n=== 8. USUWANIE ===" -ForegroundColor Cyan
Test "Usun karte 1"    DELETE "/bank/cards/$cn1"
Test "Pobierz usunieta karte -> 404"  GET "/bank/cards/$cn1" -expectCode 404

Test "Usun klienta 2"  DELETE "/bank/clients/$id2"
Test "Pobierz usunietego klienta -> 404"  GET "/bank/clients/$id2" -expectCode 404

# ─────────────────────────────────────────────────────────────────────────────
$color = if ($fail -eq 0) { "Green" } else { "Yellow" }
Write-Host "`n=================================" -ForegroundColor DarkGray
Write-Host "Wynik: $pass OK, $fail FAIL" -ForegroundColor $color
