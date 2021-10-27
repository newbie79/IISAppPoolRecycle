using System;
using Microsoft.Web.Administration;

namespace IISAppPoolRecycle
{
    class Program
    {
        private const int ERROR_SUCCESS = 0;
        private const int ERROR_INVALILD_COMMAND_LINE = 1;
        private const int ERROR_BAD_ARGUMENTS = 2;
        private const int ERROR_UNKNOWN_ERROR = 3;

        static int Main(string[] args)
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine(String.Format("IIS Application Pool Recycle (v{0}.{1}.{2:#0}.{3:#0})\n\n", version.Major, version.Minor, version.Build, version.Revision));

            if (args.Length == 0)
            {
                ShowUsage();
                return ERROR_INVALILD_COMMAND_LINE;
            }

            return Recycle(args[0]);
        }

        static int Recycle(string appPoolName)
        {
            try
            {
                ApplicationPool appPool = null;
                ServerManager serverManager = new ServerManager();
                ApplicationPoolCollection appPools = serverManager.ApplicationPools;
                foreach (ApplicationPool ap in appPools)
                {
                    if (ap.Name.Equals(appPoolName))
                    {
                        appPool = ap;
                        break;
                    }
                }

                if (appPool == null)
                {
                    Console.WriteLine(String.Format("[{0}] not found.", appPoolName));
                    return ERROR_BAD_ARGUMENTS;
                }

                if (appPool.State != ObjectState.Started)
                {
                    switch (appPool.State)
                    {
                        case ObjectState.Starting:
                            Console.WriteLine(String.Format("Error: [{0}] is starting.", appPoolName));
                            break;
                        case ObjectState.Stopping:
                            Console.WriteLine(String.Format("Error: [{0}] is stopping.", appPoolName));
                            break;
                        case ObjectState.Stopped:
                            Console.WriteLine(String.Format("Error: [{0}] is stopped.", appPoolName));
                            break;
                        default:
                            Console.WriteLine(String.Format("Error: [{0}] is unknown state.", appPoolName));
                            break;
                    }
                    return ERROR_UNKNOWN_ERROR;
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendFormat("[{0}]\n", appPool.Name);
                sb.AppendFormat("FrameworkVersion: {0}\n", appPool.ManagedRuntimeVersion);
                sb.AppendFormat("State: {0}\n", appPool.State);
                sb.AppendFormat("MaxProcesses: {0}\n", appPool.ProcessModel.MaxProcesses.ToString());
                sb.AppendFormat("WorkerProcesses: {0}\n", appPool.WorkerProcesses.Count);
                foreach (WorkerProcess workerProcess in appPool.WorkerProcesses)
                {
                    sb.AppendFormat("    {0}\n", workerProcess.ToString());
                }
                sb.AppendFormat("CPU limit: {0}\n", appPool.Cpu.Limit.ToString());
                sb.AppendFormat("Restart time: {0}\n\n", appPool.Recycling.PeriodicRestart.Time.TotalMinutes);
                Console.Write(sb.ToString());

                ObjectState newState = appPool.Recycle();
                if (newState != ObjectState.Started)
                {
                    Console.WriteLine("Failed ! (State:{0})", newState);
                    return ERROR_UNKNOWN_ERROR;
                }

                Console.WriteLine("Recycled!");
                return ERROR_SUCCESS;
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Error: {0}", ex.Message));
                return ERROR_UNKNOWN_ERROR;
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine(@"
  Usage: IISAppPoolRecycle.exe application pool name
     Ex: IISAppPoolRecycle.exe DefaultAppPool
");
        }
    }
}
