
$COM = "COM4"
$port = new-Object System.IO.Ports.SerialPort $COM,9600,None,8,one
$stop = $false

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
            $count = 0
            do {
                Start-Sleep -Milliseconds 100
                $count += 1
            } while (($stop -eq $false) -and ($count -le 100))
            $stop = $false
            Write-Host "Sending: done"
            $port.WriteLine("done")
            Break
        }
        "%stop" {
            $stop = $true
            Break
        }
        "%start *" {
            foreach ($line in Get-Content .\TestData.csv) {
                if ($stop -eq $true) {
                    Break
                }
                Write-Host "Sending: $line"
                $port.WriteLine($line)
            }
            $stop = $false
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
            Write-Host "Other: `{$line`}"
        }
    }
    while ($port.IsOpen)
}


read-com

Read-Host -Prompt "Press Enter to exit"
