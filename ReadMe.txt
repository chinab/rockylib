1.所谓性能优化，必须是建立在测试的基础之上，ACT Test是比较爽的测试工具，比Load Runner方便，比Web Stress直观，支持脚本编程和录制登陆到注销全过程。 
  所有优化都要进行对比测试，才是评判的数字依据。 
  所以，个人认为：不做压力测试，优化是可以做，但没有数据支持，是不严谨的。

2.WCF证书验证:
服务端：
	makecert -n "CN=azure.xineapp.com" -pe -sr localmachine -ss My -a sha1 -r -sky exchange -sp "Microsoft RSA SChannel Cryptographic Provider" -sy 12
	C:\Documents and Settings\All Users\Application Data\Microsoft\Crypto\RSA\MachineKeys
	C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys
	http://msdn.microsoft.com/zh-cn/library/bfsktky3.aspx
	http://support.microsoft.com/kb/901183/zh-cn
客户端：
	certmgr -add -r localmachine -s My -c -n azure.xineapp.com -s TrustedPeople
	CertMgr.msc

3.
  a. C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe D:\Projects\GitLib\Hub\System.Agent.WinService\bin\Debug\System.Agent.WinService.exe
  b. C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /u 'path'

4. 
  a. Https反向连接获取问题；
  b. https://github.com/MSOpenTech/redis;