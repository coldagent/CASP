
$COM = "COM3"
$port = new-Object System.IO.Ports.SerialPort $COM,9600,None,8

function read-com {
    $port.Open()
    do {
        $command = Read-Host -Prompt "Enter Command"
        $command = $command -replace "`n", ""
        $port.WriteLine($command);
        Write-Host "Sending: $command"
        $line = $port.ReadLine();
        $line = $line -replace "`r",""
        Write-Host "Line: `{$line`}"
    }
    while ($port.IsOpen)
}


read-com

Read-Host -Prompt "Press Enter to exit"
