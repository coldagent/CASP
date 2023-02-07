
$COM = "COM4"
$port= new-Object System.IO.Ports.SerialPort $COM,9600,None,8,one

function receive-command {
    param (
        $command
    )

    Write-Host "Command: `{$command`}"
    switch -Wildcard ($command) 
    {
        "%handshake" {
            Write-Host "Sending: connected"
            $port.WriteLine("connected")
            Break
        }
        "%raise" {
            Break
        }
        "%lower" {
            Break
        }
        "%reset" {
            Start-Sleep -Seconds 10
            Write-Host "Sending: done"
            $port.WriteLine("done")
            Break
        }
        "%stop" {

        }
        "%start *" {
            Write-Host "Sending: 1,2,3"
            $port.WriteLine("1,2,3")
            Start-Sleep -Seconds 5
            Write-Host "Sending: done"
            $port.WriteLine("done")
            Break
        }
        Default {
            Write-Host "Unknown Command"
            Break
        }
    }
}

function read-com {
    $port.Open()
    $port.DiscardInBuffer()
    $port.DiscardOutBuffer()
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
