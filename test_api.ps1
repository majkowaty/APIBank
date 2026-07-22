$BASE = "http://majko.ddns.net:9000"
$pass = 0
$fail = 0

function Test($label, $method, $url, $body = $null) {
    $params = @{ Method = $method; Uri = "$BASE$url"; ErrorAction = "Stop" }
    if ($body) {
        $params.Body        = ($body | ConvertTo-Json)
        $params.ContentType = "application/json"
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

# ── Usuwanie ──────────────────────────────────
Write-Host "`n=== USUWANIE ===" -ForegroundColor Cyan

Test "Usun karte 1"    DELETE "/bank/cards/$cn1"
Test "Usun klienta 2"  DELETE "/bank/clients/$id2"

$params404 = @{ Method = "GET"; Uri = "$BASE/bank/clients/$id2"; ErrorAction = "Stop" }
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
        Write-Host "  [FAIL] Pobierz usunietego klienta (oczekiwano 404, dostano $code)" -ForegroundColor Red
        $fail++
    }
}

# ── Wynik ─────────────────────────────────────
Write-Host "`n---------------------------------" -ForegroundColor DarkGray
$color = if ($fail -eq 0) { "Green" } else { "Yellow" }
Write-Host "Wynik: $pass OK, $fail FAIL" -ForegroundColor $color
