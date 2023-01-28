
$COM = "COM4"

function read-com {
    $port= new-Object System.IO.Ports.SerialPort $COM,9600,None,8,one
    $port.Open()
    do {
        $line = $port.ReadLine()
        $line = $line -replace "`r",""
        if ($line[0] -eq "$") {
            Write-Host "Command: `{$line`}"
        } else {
            Write-Host "Data: `{$line`}"
        }
    }
    while ($port.IsOpen)
}

read-com

Read-Host -Prompt "Press Enter to exit"
