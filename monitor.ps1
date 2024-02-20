param(
    [Parameter(Mandatory=$true)]$Port = ""
)

Write-Output "Starting monitoring on port: $Port"
$env:IDF_PATH="C:\Users\hp\esp\esp-idf"
$env:PATH="$PATH;C:\Espressif\frameworks\esp-idf-v5.1.2\tools;C:\Users\hp\esp\esp-idf;"

C:\Expressif\python_env\idf5.1_py3.11_env\Scripts\python.exe C:\Users\hp\esp\esp-idf\tools/idf_monitor.py --port $Port -b 115200 --toolchain-prefix xtensa-esp32-elf- --target esp32 --revision 0 --force-color 
    



