
$COM = "COM4"
$port= new-Object System.IO.Ports.SerialPort $COM,9600,None,8,one

function receive-command {
    param (
        $command
    )

    Write-Host "Command: `{$command`}"
    switch ($command) 
    {
        "%handshake" {
            Write-Host "Sending: %connected"
            $port.WriteLine("%connected")
        }
        Default {
            Write-Host "Unknown Command"
        }
    }
}

function read-com {
    $port.Open()
    do {
        $line = $port.ReadLine()
        $line = $line -replace "`r",""
        if ($line[0] -eq "%") {
            receive-command -command $line
        } else {
            Write-Host "Data: `{$line`}"
        }
    }
    while ($port.IsOpen)
}


read-com

Read-Host -Prompt "Press Enter to exit"
