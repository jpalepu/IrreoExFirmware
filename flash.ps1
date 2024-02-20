param(
    [Parameter(Mandatory=$true)]$Port = "",
    [Parameter(Mandatory=$true)]$FirmwarePath = ""
)
Write-Output "Starting flash on port: $Port"
Write-Output "Firmware path: $FirmwarePath"

$env:IDF_PATH="C:\Users\hp\esp\esp-idf"
$env:PATH="$PATH;C:\Espressif\frameworks\esp-idf-v5.1.2\tools;C:\Users\hp\esp\esp-idf;"

C:\Expressif\python_env\idf5.1_py3.11_env\Scripts\python.exe C:\Users\hp\esp\esp-idf\components\esptool_py\esptool\esptool.py -p $Port -b 460800 --before default_reset --after hard_reset --chip esp32  write_flash --flash_mode dio --flash_size 8MB --flash_freq 40m 0x1000  $FirmwarePath\bootloader.bin 0x8000 $FirmwarePath\partition-table.bin 0xd000 $FirmwarePath\ota_data_initial.bin 0x10000 $FirmwarePath\irreo-esp32-control.bin
exit $LastExitCode

