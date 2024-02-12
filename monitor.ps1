param(
    [Parameter(Mandatory=$true)]$Port = "",
)

powershell -Command{
    $env:IDF_PATH="C:\Users\hp\esp\esp-idf"
    $env:PATH="C:\Windows\system32;C:\Windows;C:\Windows\System32\Wbem;C:\Windows\System32\WindowsPowerShell\v1.0\;C:\Windows\System32\OpenSSH\;C:\Program Files\Git\cmd;C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\;C:\Program Files\dotnet\;C:\Program Files\Docker\Docker\resources\bin;C:\Users\hp\AppData\Local\Programs\Python\Python312\Scripts\;C:\Users\hp\AppData\Local\Programs\Python\Python312\;C:\Users\hp\AppData\Local\Microsoft\WindowsApps;C:\msys64\mingw64\bin;C:\Users\hp\AppData\Local\Programs\Microsoft VS Code\bin;C:\Users\hp\.dotnet\tools;C:\Program Files\dotnet;C:\Espressif\frameworks\esp-idf-v5.1.2\tools;C:\Users\hp\esp\esp-idf;"
    C:\Expressif\python_env\idf5.1_py3.11_env\Scripts\python.exe C:\Users\hp\esp\esp-idf\tools/idf_monitor.py -p $Port -b 115200 --toolchain-prefix xtensa-esp32-elf- --target esp32 --revision 0 --force-color 
    
}