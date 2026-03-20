# smtp4dev start script
# SMTP : 1025
# IMAP : 1143
# POP3 : 1110
# Web UI: http://localhost:5010

$smtpPort = 1025
$imapPort = 1143
$pop3Port = 1110
$webPort  = 5010

# Kill any process listening on the target ports
foreach ($port in @($smtpPort, $imapPort, $pop3Port, $webPort)) {
    $lines = netstat -ano | Select-String ":$port " | Select-String "LISTENING"
    foreach ($line in $lines) {
        $procId = ($line.ToString().Trim() -split "\s+")[-1]
        if ($procId -match "^\d+$") {
            Stop-Process -Id ([int]$procId) -Force -ErrorAction SilentlyContinue
        }
    }
}

Start-Sleep -Milliseconds 800

Write-Host "Starting smtp4dev..."
Write-Host "  SMTP  : localhost:$smtpPort"
Write-Host "  IMAP  : localhost:$imapPort"
Write-Host "  POP3  : localhost:$pop3Port"
Write-Host "  Web UI: http://localhost:$webPort"
Write-Host ""
Write-Host "Open http://localhost:$webPort in your browser to check received emails."
Write-Host "Press Ctrl+C to stop."
Write-Host ""

smtp4dev --smtpport $smtpPort --imapport $imapPort --pop3port $pop3Port --urls "http://localhost:$webPort"
