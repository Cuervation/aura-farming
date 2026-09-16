using System.Threading.Tasks;
using System;
using System.Diagnostics; using System.Text.Json; var p=new Process{StartInfo=new ProcessStartInfo(args[1]){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true}}; for(var i=2;i<args.Length;i++)p.StartInfo.ArgumentList.Add(args[i]); p.Start(); var o=p.StandardOutput.ReadToEndAsync(); var e=p.StandardError.ReadToEndAsync(); var timeout=int.Parse(args[0]); var exitTask=p.WaitForExitAsync(); var timed=await Task.WhenAny(exitTask,Task.Delay(timeout))!=exitTask; if(timed&&!p.HasExited)p.Kill(true); Console.WriteLine(JsonSerializer.Serialize(new{success=!timed&&p.ExitCode==0,timedOut=timed,exitCode=timed?(int?)null:p.ExitCode,stdout=await o,stderr=await e})); return timed?124:p.ExitCode;



