
$COM = "COM3"
$port = new-Object System.IO.Ports.SerialPort $COM,9600,None,8

function read-com {
    $command = "%start 10"
    $port.Open()
    $port.WriteLine("%handshake")
    $port.ReadLine()
    $port.WriteLine($command)
    Write-Host "Sending: $command"
    do {
        $line = $port.ReadLine()
        $line = $line -replace "`r",""
        Write-Host "Line: `{$line`}"
    }
    while ($port.IsOpen)
}


read-com

Read-Host -Prompt "Press Enter to exit"
