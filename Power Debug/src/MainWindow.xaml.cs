using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using Excel = Microsoft.Office.Interop.Excel;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Management;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO.Compression;
using System.Data;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Linq;
using System.Xml;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace PowerDebug
{
    using OxyPlot;
    using OxyPlot.Series;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IntPtr MainWindowHandle { get; set; }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll", SetLastError = true)]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        static extern IntPtr SetActiveWindow(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        const int WM_SYSCOMMAND = 274;
        const int SC_MAXIMIZE = 61488;

        private const int WS_VISIBLE = 0x10000000;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        string DeviceInfo;
        uint count = 0;
        public bool isfirstlaunch = true;
        public bool Genuine = false;
        public String sortwith = "Ontime ASC";
        bool isGCTinitialised = false;
        bool isQuickLaunched = false;
        bool isHost = false;
        public Process procamu;
        public Process procgct;
        public UsbAdb deviceUsb;
        public bool test_wo_device = false;


        public bool cstate = false;

        public PhysCPU pCpu0, pCpu1,pCpu2,pCpu3;
        public VirtCPU Modem, Linux0, Linux1,Linux2,Linux3, TEE;
        public SoFIA soc;
        public DSA dsa;

        int DstatesCount = 0;
        int Interruptcount = 0;
        int VmmStatCount = 0;
        public Boolean initconn = false;
        String logsavepath = null;
        Boolean UsbDisable = false;

        public List<UseCaseProfile> m_UseCaseList;
        public UseCaseProfile m_SelectedUseCase;

        private LineSeries pcpu0_ic_Series;
        private LineSeries pcpu1_ic_Series;
        private LineSeries pcpu2_ic_Series;
        private LineSeries pcpu3_ic_Series;

        private LineSeries pcpu0_avgres_Series;
        private LineSeries pcpu1_avgres_Series;
        private LineSeries pcpu2_avgres_Series;
        private LineSeries pcpu3_avgres_Series;

        private LineSeries pcpu0_util_Series;
        private LineSeries pcpu1_util_Series;
        private LineSeries pcpu2_util_Series;
        private LineSeries pcpu3_util_Series;

        private LineSeries mcpu_util_Series;
        private LineSeries acpu0_util_Series;
        private LineSeries acpu1_util_Series;
        private LineSeries vmm_overhead_Series;
        private LineSeries vmm_minoverhead_Series;
        private LineSeries vmm_maxoverhead_Series;
        private LineSeries vmcs_m_Series;
        private LineSeries vmcs_acpu0_Series;
        private LineSeries vmcs_acpu1_Series;

        public PlotModel pCPU0IC { get; set; }
        public PlotModel pCPU1IC { get; set; }
        public PlotModel pCPU2IC { get; set; }
        public PlotModel pCPU3IC { get; set; }

        public PlotModel pCPU0AR { get; set; }
        public PlotModel pCPU1AR { get; set; }
        public PlotModel pCPU2AR { get; set; }
        public PlotModel pCPU3AR { get; set; }

        public PlotModel pCPU0UTIL { get; set; }
        public PlotModel pCPU1UTIL { get; set; }
        public PlotModel pCPU2UTIL { get; set; }
        public PlotModel pCPU3UTIL { get; set; }

        public PlotModel ModemCPUUTIL { get; set; }

        public PlotModel AOSCPUUTIL { get; set; }

        public PlotModel vmmOverHead { get; set; }

        public PlotModel VMCS { get; set; }


        /*
        * @Comment: This implements the DSA analysis
        */
        public class DSA
        {

            private long m_ExpectedSleepTime;

            private SoFIA m_SofiaSoc;

            public DSA(SoFIA sofiasoc)
            {
                this.m_ExpectedSleepTime = 0;
                this.m_SofiaSoc = sofiasoc;
            }

            /*
             * @comment: Perform deep sleep analysis and return status as array 
             * @todo: It would be nice to make the DSA analysis a table driven, instead of custom code
             */
            public List<DSAReasons> PerformDeepSleepAnalysis(
                    UseCaseProfile ucp,
                    List<VirtCPU> virtCPUList,
                    List<PhysCPU> phyCPUList,
                    List<IRQ> intList, List<DeviceStates> devList)
            {
                bool bValidDeepSleep = true;
                //List<DSAReasons> complainerCPU, complainerInts, complainerDevices;

                List<DSAReasons> complainerAll = new List<DSAReasons>();

                /*
                 * Check for CPU statistics
                 */
                foreach (PhysCPU pcpu in phyCPUList)
                {
                    float idleres = ((float)pcpu.GetIdleTime() / (float)pcpu.GetTotalTime()) * 100;
                    float avgidleres = (float)pcpu.GetIdleTime() / (float)pcpu.GetIdleCount();
                    DSAReasons ir = new DSAReasons();
                    DSAReasons air = new DSAReasons();
                    CPUProfile cpuProf;

                    cpuProf = ucp.FindPhysicalCoreProfile(pcpu.GetCoreInstance());

                    if (idleres > cpuProf.m_OptIdleResidency)
                    {
                        ir.SetReason("Optimal CPU resideny in deep sleep");
                        ir.SetSubsystem("CPU" + pcpu.GetCoreInstance());
                        ir.SetExpectedOutcome("Expecting " + cpuProf.m_OptIdleResidency + "% or more of CPU Idle time during usecase " + ucp.m_DisplayName);
                        ir.SetGrading("GREEN");
                        ir.SetPriority(0);
                    }
                    else if (idleres > cpuProf.m_NonOptIdleResidency)
                    {
                        ir.SetReason("Non-Optimal CPU Idle resideny in deep sleep (" + idleres.ToString("0.00") + "%)");
                        ir.SetSubsystem("CPU" + pcpu.GetCoreInstance());
                        ir.SetExpectedOutcome("Expecting " + cpuProf.m_OptIdleResidency + "% or more of CPU Idle time during usecase " + ucp.m_DisplayName);
                        ir.SetGrading("YELLOW");
                        ir.SetPriority(0);
                    }
                    else
                    {
                        ir.SetReason("Not meeting target CPU Idle resideny in deep sleep (" + idleres.ToString("0.00") + "%)");
                        ir.SetSubsystem("CPU" + pcpu.GetCoreInstance());
                        ir.SetExpectedOutcome("Expecting " + cpuProf.m_OptIdleResidency + "% or more of CPU Idle time during usecase " + ucp.m_DisplayName);
                        ir.SetGrading("RED");
                        ir.SetPriority(0);
                    }

                    complainerAll.Add(ir);

                    /*
                    * If Average Idle residency is less than 100ms for CPU0
                    * flag it.
                    */
                    float air_optthreshold = cpuProf.m_OptAverageIdleResidency;
                    float air_nonoptthreshold = cpuProf.m_NonOptAverageIdleResidency;

                    if (avgidleres < air_nonoptthreshold)
                    {
                        air.SetReason("Average Idle residency in C6 is low. Average C6 residency is " + avgidleres.ToString("0.00") + "ms");
                        air.SetSubsystem("CPU" + pcpu.GetCoreInstance());
                        air.SetExpectedOutcome("Average C6 residency is " + air_optthreshold.ToString("0.00") + "ms or more");
                        air.SetGrading("RED");
                        air.SetPriority(0);
                    }
                    else if (avgidleres < air_optthreshold)
                    {
                        air.SetReason("Average Idle residency in C6 is low. Average C6 residency is " + avgidleres.ToString("0.00") + "ms");
                        air.SetSubsystem("CPU" + pcpu.GetCoreInstance());
                        air.SetExpectedOutcome("Average C6 residency is " + air_optthreshold.ToString("0.00") + "ms or more");
                        air.SetGrading("YELLOW");
                        air.SetPriority(0);
                    }
                    complainerAll.Add(air);
                }

                /*
                 * Check if the desired sleep time and actual sleep time matches
                 */
                foreach (PhysCPU pcpu in phyCPUList)
                {
                    DSAReasons runtime = new DSAReasons();
                    /*
                     * Check if the Actual run time and expected run time matches
                     */
                    if (!CompareTime(pcpu.GetTotalTime(), this.m_ExpectedSleepTime))
                    {
                        runtime.SetReason("Sleep time does not match expectation.");
                        runtime.SetSubsystem("General");
                        runtime.SetExpectedOutcome("Expected sleep time " + (m_ExpectedSleepTime / 1000) + " seconds but did only " + (pcpu.GetTotalTime() / 1000) + " seconds");
                        runtime.SetGrading("RED");
                        runtime.SetPriority(0);
                        complainerAll.Add(runtime);
                        break;
                    }
                }

                /*
                 * Check if the actual sleep time is too small for real meaningful analysis
                 */
                foreach (PhysCPU pcpu in phyCPUList)
                {
                    DSAReasons runtime = new DSAReasons();
                    /*
                     * Check if the Actual run time and expected run time matches
                     */
                    if (pcpu.GetTotalTime() <= 15 * 60 * 1000)
                    {
                        runtime.SetReason("Sleep time is too short.");
                        runtime.SetSubsystem("General");
                        runtime.SetExpectedOutcome("Sleep time should be more than 30 minutes ");
                        runtime.SetGrading("RED");
                        runtime.SetPriority(0);
                        complainerAll.Add(runtime);
                        break;
                    }
                    else if (pcpu.GetTotalTime() < (30 * 60 * 1000 - 1))
                    {
                        runtime.SetReason("Sleep time is less than ideal.");
                        runtime.SetSubsystem("General");
                        runtime.SetExpectedOutcome("Sleep time should be more than 30 minutes ");
                        runtime.SetGrading("YELLOW");
                        runtime.SetPriority(1);
                        complainerAll.Add(runtime);
                        break;
                    }
                }

                /*
                 * Interrupt processing
                 */

                /*
                 * Build the array of active, idle time for each Physical core
                 */
                long[] pcpu_idle = new long[phyCPUList.Count];
                long[] pcpu_active = new long[phyCPUList.Count];
                long[] pcpu_total = new long[phyCPUList.Count];

                foreach (PhysCPU pcpu in phyCPUList)
                {
                    pcpu_idle[pcpu.GetCoreInstance()] = pcpu.GetIdleTime();
                    pcpu_active[pcpu.GetCoreInstance()] = pcpu.GetActiveTime();
                    pcpu_total[pcpu.GetCoreInstance()] = pcpu.GetTotalTime();
                }

                IRQ audio = FindIRQByName(intList, "INT_LINE_DMA1_CH0_7");

                if (audio != null)
                {
                    int irqOwner = m_SofiaSoc.GetVectorOwner(audio.GetIrqNumber());
                    int cpuAffinity = 0;
                    /*
                     * Modem interrupt
                     * @todo: this is hard coded and needs to be handled generically
                     */
                    if (irqOwner == 1)
                    {
                        cpuAffinity = 0;
                    }

                    /*
                     * Android interrupt
                     * @todo: this is hard coded and needs to be handled generically
                     */
                    if (irqOwner == 2)
                    {
                        cpuAffinity = 1;
                    }

                    long avgIntRate = pcpu_total[cpuAffinity] / audio.GetTotalInterruptCounts();

                    /*
                     * If interrupt rate is less than 30ms, then there is an audio playback
                     */
                    if (avgIntRate < 30)
                    {
                        DSAReasons intReason = new DSAReasons();
                        string reason;
                        reason = "Low Power Audio playback detected \n";
                        intReason.SetReason(reason);
                        intReason.SetSubsystem("General");
                        intReason.SetExpectedOutcome("If Audio playback is being tested, then this is normal");
                        intReason.SetGrading("YELLOW");
                        intReason.SetPriority(0);
                        complainerAll.Add(intReason);

                        intList.Remove(audio);
                    }

                }


                Dictionary<string, long> predefIntDict = new Dictionary<string, long>();

                //predefIntDict.Add("INT_LINE_STM0", 1000);
                //predefIntDict.Add("INT_LINE_STM1", 2000);
                //predefIntDict.Add("INT_LINE_CC0_CCINT0_7", 2000);
                //predefIntDict.Add("INT_LINE_GSI_GP0", 500);
                //predefIntDict.Add("INT_LINE_GSI_T_INT2", 500);

                foreach (string intName in predefIntDict.Keys)
                {
                    int cpuAffinity = 0;

                    IRQ irq = FindIRQByName(intList, intName);

                    int irqOwner = m_SofiaSoc.GetVectorOwner(irq.GetIrqNumber());

                    long threshIntRate = predefIntDict[intName];

                    /*
                     * Modem interrupt
                     * @todo: this is hard coded and needs to be handled generically
                     */
                    if (irqOwner == 1)
                    {
                        cpuAffinity = 0;
                    }

                    /*
                     * Android interrupt
                     * @todo: this is hard coded and needs to be handled generically
                     */
                    if (irqOwner == 2)
                    {
                        cpuAffinity = 1;
                    }

                    long avgIntRate = pcpu_total[cpuAffinity] / irq.GetInterruptCount(cpuAffinity);

                    if (avgIntRate < threshIntRate)
                    {
                        DSAReasons intReason = new DSAReasons();
                        string reason;
                        reason = "Too many interrupts from line " + intName + "\n";
                        reason += "interrupt rate=" + avgIntRate + "ms per interrupt";
                        intReason.SetReason(reason);
                        intReason.SetSubsystem("Interrupts");
                        intReason.SetExpectedOutcome("More than one interrupt per " + threshIntRate + "ms");
                        intReason.SetGrading("RED");
                        intReason.SetPriority(0);
                        complainerAll.Add(intReason);
                    }

                    /*
                     * Remove this IRQ from list
                     */
                    //intList.Remove(irq);
                }

                foreach (IRQ irq in intList)
                {
                    int cpuAffinity = 0;

                    int irqOwner = m_SofiaSoc.GetVectorOwner(irq.GetIrqNumber());

                    long threshIntRate = 0;

                    /*
                     * Modem interrupt
                     * @todo: this is hard coded and needs to be handled generically
                     */
                    if (irqOwner == 1)
                    {
                        cpuAffinity = 0;
                        threshIntRate = 90; //90 ms, no hard coding please
                    }

                    /*
                     * Android interrupt
                     * @todo: this is hard coded and needs to be handled generically
                     */
                    if (irqOwner == 2)
                    {
                        cpuAffinity = 1;
                        threshIntRate = 240; //100 ms, no hard coding please
                    }

                    if (irq.GetTotalWakeInterruptCounts() == 0)
                    {
                        continue;
                    }

                    float avgIntRate = (float)pcpu_total[cpuAffinity] / (float)irq.GetInterruptCount(cpuAffinity);

                    if (avgIntRate < threshIntRate)
                    {
                        DSAReasons intReason = new DSAReasons();
                        string reason;
                        reason = "Too many interrupts from line " + irq.GetIrqName() + "\n";
                        reason += "interrupt rate=" + avgIntRate.ToString("0.00") + "ms per interrupt";
                        intReason.SetReason(reason);
                        intReason.SetSubsystem("Interrupts");
                        intReason.SetExpectedOutcome("More than one interrupt per " + threshIntRate + "ms");
                        intReason.SetGrading("RED");
                        intReason.SetPriority(0);
                        complainerAll.Add(intReason);
                    }

                    /*
                     * Remove this IRQ from list
                     */
                    //intList.Remove(irq);

                }

                /*
                 * Device statistics 
                 */

                DeviceStates fmr = FindDeviceByName(devList, "ABB_FMR");

                /*
                 * If FM Radio device is active for more than 90% of total time
                 * then FM Radio playback is active
                 */
                if (fmr.GetOnTime() > (long)((float)pcpu_total[0] * 0.90))
                {
                    DSAReasons fm_reasons = new DSAReasons();
                    /*
                     * Check if the Actual run time and expected run time matches
                     */
                    fm_reasons.SetReason("FM Radio play is active");
                    fm_reasons.SetSubsystem("Devices");
                    fm_reasons.SetExpectedOutcome("If FM Radio playback is expected, this is optimal");
                    fm_reasons.SetGrading("YELLOW");
                    fm_reasons.SetPriority(0);
                    complainerAll.Add(fm_reasons);
                }

                DeviceStates wlan = FindDeviceByName(devList, "ABB_WLAN");

                /*
                 * If WLAN device is active for more than 90% of total time
                 * then FM Radio playback is active
                 */
                if (wlan.GetOnTime() > (long)((float)pcpu_total[0] * 0.90))
                {
                    DSAReasons wlan_reasons = new DSAReasons();
                    float wlantime = ((float)wlan.GetOnTime() / (float)pcpu_total[1]) * 100;
                    /*
                     * Check if the Actual run time and expected run time matches
                     */
                    wlan_reasons.SetReason("WLAN is active (" + wlantime.ToString("0.00") + "%)");
                    wlan_reasons.SetSubsystem("Devices");
                    wlan_reasons.SetExpectedOutcome("WLAN should be turned off in Deep Sleep");
                    wlan_reasons.SetGrading("RED");
                    wlan_reasons.SetPriority(0);
                    complainerAll.Add(wlan_reasons);
                }

                return complainerAll;

            }

            /*
             * @comment Find a device node from the list which matches the give device name
             */
            private static DeviceStates FindDeviceByName(List<DeviceStates> devList, string name)
            {
                return devList.Find(x => String.Compare(x.getdevName(), name) == 0);
            }


            /*
             * @comment Find a IRQ node from the list which matches the give IRQ name
             */
            private static IRQ FindIRQByName(List<IRQ> irqList, string name)
            {
                return irqList.Find(x => String.Compare(x.GetIrqName(), name) == 0);
            }
            /*
             * @comment Compare two times and see if they match within 8 seconds of each other
             */
            private bool CompareTime(long t1, long t2)
            {
                if (Math.Abs(t1 - t2) < 8000)
                    return true;

                return false;
            }
            /*
             * @comment: Expected sleep time for the run. Used to cross-verify from the data
             * in the system to confirm, if what we slept is what we expected to sleep 
             * (Yawnn....)
             */
            public void SetSleepTime(long seconds)
            {
                this.m_ExpectedSleepTime = seconds * 1000;
            }

            public long GetSleepTime()
            {
                return this.m_ExpectedSleepTime;
            }

            /*
             * @comment: Create a Virtual CPU delta list before and after sleep
             */
            public static List<VirtCPU> ParseVirtualCPUStats(SoFIA soc)
            {
                String[] prelines = VirtCPU.ReadFile("beforedsa\\prevcpu.txt");
                String[] postlines = VirtCPU.ReadFile("afterdsa\\postvcpu.txt");
                String[] vm = { "mex", "linux", "secvm" };

                int vcpu_total = 2 + 1 + 1; // this needs to be dynamic

                VirtCPU[] vPreCpu, vPostCpu, deltavCpu;

                vPreCpu = new VirtCPU[vcpu_total];
                vPostCpu = new VirtCPU[vcpu_total];
                deltavCpu = new VirtCPU[vcpu_total];

                for (int i = 0; i < vcpu_total; i++)
                {
                    if (i == 0)
                    {
                        vPreCpu[i] = VirtCPU.ParseFile(prelines, "mex", 0);
                        vPostCpu[i] = VirtCPU.ParseFile(postlines, "mex", 0);

                        vPreCpu[i].SetVMName("Modem");
                        vPreCpu[i].SetCoreInstance(0);

                        vPostCpu[i].SetVMName("Modem");
                        vPostCpu[i].SetCoreInstance(0);
                    }
                    if (i == 1 || i == 2)
                    {
                        vPreCpu[i] = VirtCPU.ParseFile(prelines, "linux", i - 1);
                        vPostCpu[i] = VirtCPU.ParseFile(postlines, "linux", i - 1);

                        vPreCpu[i].SetVMName("Linux");
                        vPreCpu[i].SetCoreInstance(i - 1);

                        vPostCpu[i].SetVMName("Linux");
                        vPostCpu[i].SetCoreInstance(i - 1);

                    }
                    if (i == 3)
                    {
                        vPreCpu[i] = VirtCPU.ParseFile(prelines, "secvm", 0);
                        vPostCpu[i] = VirtCPU.ParseFile(postlines, "secvm", 0);

                        vPreCpu[i].SetVMName("TEE");
                        vPreCpu[i].SetCoreInstance(0);

                        vPostCpu[i].SetVMName("TEE");
                        vPostCpu[i].SetCoreInstance(0);
                    }
                }

                for (int j = 0; j < vcpu_total; j++)
                {
                    deltavCpu[j] = vPostCpu[j] - vPreCpu[j];
                }

                return deltavCpu.ToList();

            }

            /*
             * @comment: Create a Physical CPU delta list before and after sleep
             */
            public static List<PhysCPU> ParsePhysicalCPUStats(SoFIA sofia)
            {
                int max_cpus = sofia.GetSKUCPUCount();
                PhysCPU[] preCpu, postCpu, deltaPCPU;

                preCpu = new PhysCPU[max_cpus];
                postCpu = new PhysCPU[max_cpus];
                deltaPCPU = new PhysCPU[max_cpus];

                for (int cpu = 0; cpu < max_cpus; cpu++)
                {
                    preCpu[cpu] = PhysCPU.ParseFile("beforedsa\\prepcpu.txt", cpu);
                    postCpu[cpu] = PhysCPU.ParseFile("afterdsa\\postpcpu.txt", cpu);

                    preCpu[cpu].SetCoreInstance(cpu);
                    postCpu[cpu].SetCoreInstance(cpu);

                    deltaPCPU[cpu] = postCpu[cpu] - preCpu[cpu];
                }

                return deltaPCPU.ToList();
            }

            /*
             * @comment: Create a Interrupt delta list before and after sleep
             */
            public static List<IRQ> ParseInterrupts(SoFIA sofia)
            {
                string[] prelines = IRQ.ReadFile("beforedsa\\predsairq.txt");
                string[] postlines = IRQ.ReadFile("afterdsa\\postdsairq.txt");
                IRQCompare irqComporator = new IRQCompare();
                List<IRQ> preirqstats, postirqstats;
                List<IRQ> deltaList, commonList;

                preirqstats = new List<IRQ>();
                postirqstats = new List<IRQ>();

                preirqstats = IRQ.ParseFile(prelines, sofia);
                postirqstats = IRQ.ParseFile(postlines, sofia);

                deltaList = new List<IRQ>();

                /*
                 * Add all new elements in the Post sleep list into the delta list
                 */
                deltaList = postirqstats.Except(preirqstats, irqComporator).ToList();

                /*
                 * Find the common elements in both the list
                 */
                commonList = postirqstats.Intersect(preirqstats, irqComporator).ToList();

                /*
                 * Find the interrupt and wake count delta between pre and post deep sleep
                 */
                foreach (IRQ irq in commonList)
                {
                    IRQ deltaInt = new IRQ(sofia);
                    IRQ lhs, rhs;

                    int irqno = irq.GetIrqNumber();

                    rhs = preirqstats.Find(x => (x.GetIrqNumber() == irqno));
                    lhs = postirqstats.Find(x => (x.GetIrqNumber() == irqno));

                    deltaInt = lhs - rhs;

                    deltaList.Add(deltaInt);

                }

                return deltaList;
            }

            public static List<DeviceStates> ParseDeepSleepDeviceStates()
            {
                string[] prelines = DeviceStates.ReadFile("beforedsa\\predstates.txt");
                string[] postlines = DeviceStates.ReadFile("afterdsa\\postdstates.txt");
                int prelength = prelines.Length;
                int postlength = postlines.Length;
                DeviceStates[] preState, poststate, deltastate;
                int getindex, minlen;

                preState = new DeviceStates[prelength];

                poststate = new DeviceStates[postlength];

                minlen = Math.Min(prelength - 4, postlength - 4);

                deltastate = new DeviceStates[minlen];

                for (int i = 0; i < prelength - 4; i++)
                {
                    preState[i] = DeviceStates.ParseFile(prelines, i + 4);
                }

                for (int i = 0; i < postlength - 4; i++)
                {
                    poststate[i] = DeviceStates.ParseFile(postlines, i + 4);
                }

                for (int j = 0; j < minlen; j++)
                {
                    /*
                     * Find matching entry in post device states statss
                     */
                    getindex = GetIndex(poststate, preState[j].getdevName());

                    if (getindex < 0)
                        continue;

                    deltastate[j] = poststate[getindex] - preState[j];
                }

                return deltastate.ToList();
            }

            /*
             * Get an Post device state instance with a given name
             */
            public static int GetIndex(DeviceStates[] postDev, String name)
            {
                int i = -1;

                for (i = 0; i < postDev.Length; i++)
                {
                    if (String.Compare(postDev[i].getdevName(), name, true) == 0)
                    {
                        break;
                    }
                }

                return i;
            }


        }

        /*
         * @comment: Contains the Reasons/Analysis for DSA results
         * each instance identifies a perticular issue
         */
        public class DSAReasons
        {
            /*
             * @comment: Areas/Subsystems where issue is seen
             */
            private string m_Subsystem;

            /*
             * @comment: Reason for issue
             */
            private string m_Reason;

            /*
             * @comment: Expected results
             */
            private string m_ExpectedResults;

            /*
             * @comment: Result Grading (Red, Yellow, Green)
             */
            private string m_Grading;

            /*
             * @comment: Priority of the issue. Lower number means higher priority.
             */
            private int m_Priority;

            public void SetReason(string reason)
            {
                this.m_Reason = reason;
            }

            public void SetGrading(string grade)
            {
                this.m_Grading = grade;
            }

            public void SetSubsystem(string area)
            {
                this.m_Subsystem = area;
            }

            public void SetExpectedOutcome(string outcome)
            {
                this.m_ExpectedResults = outcome;
            }

            public void SetPriority(int prio)
            {
                this.m_Priority = prio;
            }

            public string GetReason()
            {
                return this.m_Reason;
            }

            public string GetGrading()
            {
                return this.m_Grading;
            }

            public string GetSubsystem()
            {
                return this.m_Subsystem;
            }

            public string GetExpectedOutcome()
            {
                return this.m_ExpectedResults;
            }

            public int GetPriority()
            {
                return this.m_Priority;
            }
        }

        public class UsbAdb
        {
            private ManagementEventWatcher watcherInsertNewDevice;
            private ManagementEventWatcher watcherRemoveDevice;
            private bool bIsConnected;

            public event EventHandler UpdateConnectionStatus;

            public enum DSAFSM { IDLE, WAIT_DSLEEP_ENTER, DSLEEP_ENTER_ABORT, IN_DSLEEP, DSLEEP_DONE, DSLEEP_NOT_RECOVERED };

            public int dsaState;

            public int getDsaState()
            {
                return dsaState;
            }

            public void setDsaState(int state)
            {
                dsaState = state;
                UpdateConnectionStatus(this, new EventArgs());
            }


            public List<USBDeviceInfo> GetUSBDevices()
            {
                List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

                ManagementObjectCollection collection;
                using (var searcher = new ManagementObjectSearcher(@"Select *    From Win32_USBHub"))
                    collection = searcher.Get();

                foreach (var device in collection)
                {
                    devices.Add(new USBDeviceInfo(
                    (string)device.GetPropertyValue("DeviceID"),
                    (string)device.GetPropertyValue("PNPDeviceID"),
                    (string)device.GetPropertyValue("Description")
                    ));
                }
                collection.Dispose();
                return devices;
            }
            private void watcher_EventArrived(object sender, EventArrivedEventArgs e)
            {
                ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                string deviceID = (string)instance["DeviceID"];
                string deviceName = (string)instance["Name"];

                if (deviceID.Contains("IMC1&VID_8087&PID_0928&CF_00&MI_01"))
                {
                    bIsConnected = true;
                    if (this.getDsaState() == (int)UsbAdb.DSAFSM.IN_DSLEEP)
                    {
                        /*
                         * We did not enter deep sleep and timer aborted us
                         * This would also take care of updating the status bar
                         */
                        this.setDsaState((int)UsbAdb.DSAFSM.DSLEEP_DONE);
                    }
                    else
                        UpdateConnectionStatus(this, new EventArgs());
                }
            }

            private void watcher_EventRemoved(object sender, EventArrivedEventArgs e)
            {
                ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                string deviceID = (string)instance["DeviceID"];
                string deviceName = (string)instance["Name"];

                if (deviceID.Contains("IMC1&VID_8087&PID_0928&CF_00&MI_01"))
                {
                    //"SoFIA device disconnected"
                    bIsConnected = false;
                    if (this.getDsaState() == (int)UsbAdb.DSAFSM.WAIT_DSLEEP_ENTER)
                    {
                        /*
                         * We entered deep sleep
                         */
                        this.setDsaState((int)UsbAdb.DSAFSM.IN_DSLEEP);
                    }
                    else
                        UpdateConnectionStatus(this, new EventArgs());
                }
            }

            public void Init()
            {
                watcherInsertNewDevice = new ManagementEventWatcher();
                var queryNew = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent " +
                                                    "WITHIN 2 "
                                                    + "WHERE TargetInstance ISA 'Win32_PnPEntity'");
                watcherInsertNewDevice.EventArrived += new EventArrivedEventHandler(watcher_EventArrived);
                watcherInsertNewDevice.Query = queryNew;
                watcherInsertNewDevice.Start();

                watcherRemoveDevice = new ManagementEventWatcher();
                var queryRemove = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent " +
                                                    "WITHIN 2 "
                                                    + "WHERE TargetInstance ISA 'Win32_PnPEntity'");
                watcherRemoveDevice.EventArrived += new EventArrivedEventHandler(watcher_EventRemoved);
                watcherRemoveDevice.Query = queryRemove;
                watcherRemoveDevice.Start();

                bIsConnected = false;
                dsaState = (int)DSAFSM.IDLE;
            }

            public bool isConnected(string device)
            {
                return bIsConnected;
            }

            public Boolean CheckSofiaConnected(Boolean connection)
            {
                var usbDevices = GetUSBDevices();
                foreach (var usbDevice in usbDevices)
                {
                    if (usbDevice.DeviceID.Contains("VID_8087&PID_0928"))
                    {
                        connection = true;
                        bIsConnected = true;
                    }
                }
                return connection;

            }
        }

        public class USBDeviceInfo
        {
            public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
            {
                this.DeviceID = deviceID;
                this.PnpDeviceID = pnpDeviceID;
                this.Description = description;
            }
            public string DeviceID { get; set; }
            public string PnpDeviceID { get; set; }
            public string Description { get; set; }
        }

        public class DeviceStates
        {
            String devName, State;
            long ontime, offtime;

            DeviceStates()
            {
                devName = State = null;
                ontime = offtime = 0;
            }
            public void setontime(long time)
            {
                ontime = time;
            }
            public void setofftime(long time)
            {
                offtime = time;
            }
            public void SetName(String name)
            {
                devName = name;

            }
            public void setstate(string state)
            {
                State = state;
            }

            public string getdevName()
            {
                return devName;
            }

            public long GetOnTime()
            {
                return ontime;
            }

            public long GetOffTime()
            {
                return offtime;
            }

            public static DeviceStates operator -(DeviceStates lhs, DeviceStates rhs)
            {
                DeviceStates delta = new DeviceStates();
                delta.setontime(lhs.ontime - rhs.ontime);
                delta.setofftime(lhs.offtime - rhs.offtime);
                delta.SetName(lhs.getdevName());
                return delta;
            }
            public static string[] ReadFile(string file)
            {
                string fileName = AppDomain.CurrentDomain.BaseDirectory + file;
                string[] lines = System.IO.File.ReadAllLines(fileName);
                return lines;
            }

            public static DeviceStates ParseFile(string[] lines, int lineno)
            {
                return Parse_DevState_Stats(lines[lineno]);
            }


            private static DeviceStates Parse_DevState_Stats(string line)
            {
                // MessageBox.Show("enterd split------");
                string[] split_name = new string[64];
                split_name = line.Split(',');

                DeviceStates DevStates = new DeviceStates();
                DevStates.setontime(Int64.Parse(split_name[2].Trim()));
                DevStates.setofftime(Int64.Parse(split_name[3].Trim()));
                DevStates.SetName(split_name[0]);
                DevStates.setstate(split_name[1]);
                return DevStates;
            }
        }

        public class IRQ
        {
            string m_IrqName;
            int Irqnumber;
            long Starttime, LogTime;
            long[] IntCount;
            long[] WakeCount;
            int cpucount = 0;

            public IRQ(SoFIA soc)
            {
                int maxcpus =Int32.Parse(GetCount());

                m_IrqName = null;

                Irqnumber = 0;

                Starttime = LogTime = 0;

                IntCount = new long[maxcpus];

                WakeCount = new long[maxcpus];

                cpucount = maxcpus;

                for (int i = 0; i < maxcpus; i++)
                {
                    IntCount[i] = WakeCount[i] = 0;
                }
            }

            public IRQ(int maxcpus)
            {
                m_IrqName = null;

                Irqnumber = 0;

                Starttime = LogTime = 0;

                IntCount = new long[maxcpus];

                WakeCount = new long[maxcpus];

                cpucount = maxcpus;

                for (int i = 0; i < maxcpus; i++)
                {
                    IntCount[i] = WakeCount[i] = 0;
                }

            }

            public void SetStartTime(long time)
            {
                Starttime = time;
            }
            public void SetLogTime(long time)
            {
                LogTime = time;
            }
            public void SetInterruptCount(long time, int cpu)
            {
                IntCount[cpu] = time;
            }

            public void SetWakeCount(long time, int cpu)
            {
                WakeCount[cpu] = time;
            }

            public int GetCPUCount()
            {
                return this.cpucount;
            }

            public int GetIrqNumber()
            {
                return this.Irqnumber;
            }

            public string GetIrqName()
            {
                return this.m_IrqName;
            }
            /*
             * @Comment: Given a CPU return the Interrupt counts for this vector
             */
            public long GetInterruptCount(int cpu)
            {
                if (cpu >= cpucount)
                    return 0;
                else
                    return IntCount[cpu];
            }

            /*
             * @Comment: Given a CPU return the Wakeup Interrupt counts for this vector
             */
            public long GetWakeInterruptCount(int cpu)
            {
                if (cpu >= cpucount)
                    return 0;
                else
                    return WakeCount[cpu];
            }

            /*
             * @Comment: Return the Wakeup Interrupt counts for this vector for all CPUs
             */
            public long GetTotalWakeInterruptCounts()
            {
                long total = 0;

                for (int i = 0; i < cpucount; i++)
                {
                    total += WakeCount[i];
                }

                return total;
            }

            /*
             * @Comment: Return the Interrupt counts for this vector for all CPUs
             */
            public long GetTotalInterruptCounts()
            {
                long total = 0;

                for (int i = 0; i < cpucount; i++)
                {
                    total += IntCount[i];
                }

                return total;
            }


            public static IRQ operator -(IRQ lhs, IRQ rhs)
            {
                IRQ delta = new IRQ(lhs.GetCPUCount());

                delta.SetStartTime(lhs.Starttime);

                delta.SetLogTime(lhs.LogTime);

                if (lhs.Irqnumber != rhs.Irqnumber)
                {
                    delta.Irqnumber = -1;
                    return delta;
                }

                delta.Irqnumber = lhs.Irqnumber;

                delta.m_IrqName = lhs.m_IrqName;

                for (int i = 0; i < lhs.GetCPUCount(); i++)
                {
                    delta.SetInterruptCount(lhs.IntCount[i] - rhs.IntCount[i], i);
                    delta.SetWakeCount(lhs.WakeCount[i] - rhs.WakeCount[i], i);
                }
                return delta;
            }

            public static string[] ReadFile(string file)
            {
               
                string fileName = AppDomain.CurrentDomain.BaseDirectory + file;
                string[] lines = System.IO.File.ReadAllLines(fileName);            
                return lines;
            }

            public static List<IRQ> ParseFile(string[] lines, SoFIA soc)
            {
                List<IRQ> irqList = new List<IRQ>();

                IRQ irq = new IRQ(soc);

                for (int i =0; i < lines.Length; i++)
                {
                    irq = parse_irq_stats(lines[i], soc);
                   //MessageBox.Show("line is "+lines[i]);
                    /*
                     * Skip IRQ 0, as this does not exist
                     */
                    if (irq != null)
                        irqList.Add(irq);
                }
                return irqList;
            }

            private static IRQ parse_irq_stats(string line, SoFIA soc)
            {
                int index = 0;
                string[] split_name = new string[64];

                split_name = line.Split(',');
               // MessageBox.Show("" + soc.GetSKUCPUCount());
                IRQ ints = new IRQ(soc);

                ints.Irqnumber = Int32.Parse(split_name[index++].Trim());
                //MessageBox.Show(""+ints.Irqnumber);


                if (ints.Irqnumber == 0)
                    return null;

                ints.SetStartTime(Int64.Parse(split_name[index++].Trim()));
                ints.SetLogTime(Int64.Parse(split_name[index++].Trim()));

                ints.m_IrqName = soc.GetVectorName(ints.Irqnumber);
               // MessageBox.Show("" + soc.GetSKUCPUCount());

                for (int i = 0; i < soc.GetSKUCPUCount(); i++)
                {
                  // MessageBox.Show("COunt will  be----" + soc.GetSKUCPUCount());
                    ints.SetInterruptCount(Int64.Parse(split_name[index++].Trim()), i);
                }

                for (int i = 0; i < soc.GetSKUCPUCount(); i++)
                {
                    ints.SetWakeCount(Int64.Parse(split_name[index++].Trim()), i);
                }

                return ints;
            }
        }

        class IRQCompare : IEqualityComparer<IRQ>
        {
            public bool Equals(IRQ x, IRQ y)
            {
                return (x.GetIrqNumber() == y.GetIrqNumber());
            }
            public int GetHashCode(IRQ irq)
            {
                if (irq == null)
                {
                    throw new ArgumentNullException("irq");
                }
                //return irq.GetIrqNumber();
                return 0;
            }
        }

        /*
         * @Comment: Base class for all SoFIA devices
         */
        public class SoFIA
        {
            /*
             * @Comment: Max CPU count for SoFIA 3G
             */
            public int MaxCPUCount;

            /*
             * @Comment: SKU defined MAX CPU for SoFIA 3G
             */
            public int MaxSKUCPUCount;

            /*
             * @Comment: SKU defined MAX CPU for SoFIA 3G
             */
            public int MaxInterrupts;

            /*
             * @Comment: Mapping of vector name to IRQ name
             */
            public Dictionary<int, string> vectorNameDictionary;

            /*
             * @Comment: Mapping of vector name to VM owner
             */
            public Dictionary<int, int> vectorOwnerDictionary;


            public SoFIA()
            {
                vectorNameDictionary = new Dictionary<int, string>();
                vectorOwnerDictionary = new Dictionary<int, int>();
            }

            public int GetSKUCPUCount()
            {
                return this.MaxSKUCPUCount;
            }

            public virtual void LoadVectorDefinition(string fileName)
            {

            }

            public virtual string GetVectorName(int irqNumber)
            {
                return null;
            }

            public virtual int GetVectorOwner(int irqNumber)
            {
                return -1;
            }

            public virtual float GetFloatParams(string ParamName, int CpuInst)
            {
                return (float)0.0;
            }

        }

        public class SoFIA3G : SoFIA
        {
            public SoFIA3G()
            {
                //MaxCPUCount = Int32.Parse(GetCount());
               // MaxSKUCPUCount = Int32.Parse(GetCount());
                MaxInterrupts = 256;
            }

            public override void LoadVectorDefinition(string fileName)
            {
                System.Xml.Linq.XDocument xmlfile = System.Xml.Linq.XDocument.Load("");
               var nodes = xmlfile.Element("vectors").Elements("vector");

                foreach (System.Xml.Linq.XElement item in nodes)
                {
                  
                    int irqno = Int32.Parse(item.Attribute("number").Value.Trim());
                    string irqdevice = item.Attribute("comment").Value.Trim();
                    int irqVMOwner = Int32.Parse(item.Attribute("owner").Value.Trim());
                    vectorNameDictionary.Add(irqno, irqdevice);
                    vectorOwnerDictionary.Add(irqno, irqVMOwner);
                }

            }

            public override string GetVectorName(int irqNumber)
            {
                return vectorNameDictionary.ContainsKey(irqNumber) ? vectorNameDictionary[irqNumber] : "KeyNotFound";
            }

            public override int GetVectorOwner(int irqNumber)
            {
                return vectorOwnerDictionary.ContainsKey(irqNumber) ? vectorOwnerDictionary[irqNumber] : -1;
            }

            public override float GetFloatParams(string ParamName, int CpuInst)
            {
                if (String.Compare(ParamName, "CoreDeepSleepIdleResidency", true) == 0)
                {
                    if (CpuInst == 0)
                    {
                        return (float)99.5;
                    }

                    if (CpuInst == 1)
                    {
                        return (float)99.5;
                    }
                }

                if (String.Compare(ParamName, "CoreDeepSleepAverageIdleResidency", true) == 0)
                {
                    if (CpuInst == 0)
                    {
                        return (float)100;
                    }

                    if (CpuInst == 1)
                    {
                        return (float)240;
                    }
                }

                return 0;
            }

        }


        public class PhysCPU
        {
            int CoreInstance;
            long LastIdleTimeCounter, CurrentIdleTimeCounter;
            long LastActiveTimeCounter, CurrentActiveTimeCounter;
            long LastTotalTimeCounter, CurrentTotalTimeCounter;
            long LastIdleCount, CurrentIdleCount;

            public PhysCPU()
            {
                LastIdleTimeCounter = CurrentIdleTimeCounter = 0;
                LastActiveTimeCounter = CurrentActiveTimeCounter = 0;
                LastTotalTimeCounter = CurrentTotalTimeCounter = 0;
                LastIdleCount = CurrentIdleCount = 0;
                CoreInstance = 0;
            }

            public void Init()
            {
                LastIdleTimeCounter = CurrentIdleTimeCounter = 0;
                LastActiveTimeCounter = CurrentActiveTimeCounter = 0;
                LastTotalTimeCounter = CurrentTotalTimeCounter = 0;
                LastIdleCount = CurrentIdleCount = 0;
            }

            public void SetCoreInstance(int core)
            {
                this.CoreInstance = core;
            }

            public int GetCoreInstance()
            {
                return this.CoreInstance;
            }

            public void SetIdleTime(long counter)
            {
                LastIdleTimeCounter = CurrentIdleTimeCounter;
                CurrentIdleTimeCounter = counter;
            }
            public void SetActiveTime(long counter)
            {
                LastActiveTimeCounter = CurrentActiveTimeCounter;
                CurrentActiveTimeCounter = counter;
            }
            public void SetTotalTime(long counter)
            {
                LastTotalTimeCounter = CurrentTotalTimeCounter;
                CurrentTotalTimeCounter = counter;
            }

            public void SetIdleCount(long counter)
            {
                LastIdleCount = CurrentIdleCount;
                CurrentIdleCount = counter;
            }

            public long GetElapsedTime()
            {
                return CurrentTotalTimeCounter - LastTotalTimeCounter;
            }

            public long GetTotalTime()
            {
                return CurrentTotalTimeCounter;
            }

            /*
             * Get the Active time in Refresh window
             */

            public long GetActiveTime()
            {
                return CurrentActiveTimeCounter - LastActiveTimeCounter;
            }

            public long GetIdleTime()
            {
                return CurrentIdleTimeCounter - LastIdleTimeCounter;
            }

            public long GetIdleCount()
            {
                return CurrentIdleCount - LastIdleCount;
            }

            public int GetAverageIdleResidency()
            {
                if (GetIdleCount() > 0)
                    return (int)((GetIdleTime() * 1000) / GetIdleCount());
                else
                    return 0;
            }

            /*
             * @comment: Subtraction operator overload function 
             */
            public static PhysCPU operator -(PhysCPU lhs, PhysCPU rhs)
            {
                PhysCPU delta = new PhysCPU();
                delta.SetActiveTime(lhs.GetActiveTime() - rhs.GetActiveTime());
                delta.SetIdleCount(lhs.GetIdleCount() - rhs.GetIdleCount());
                delta.SetIdleTime(lhs.GetIdleTime() - rhs.GetIdleTime());
                delta.SetTotalTime(lhs.GetTotalTime() - rhs.GetTotalTime());

                delta.SetCoreInstance(lhs.CoreInstance);

                return delta;
            }

            /*
             * @comment: Given a file name and a physical CPU instances, parse and
             *           creates a new PhysCPU instance
             * @TODO: Better parse error handling. 
             */
            public static PhysCPU ParseFile(string file, int cpuinstance)
            {

                string fileName = AppDomain.CurrentDomain.BaseDirectory + file;
                string[] lines = System.IO.File.ReadAllLines(fileName);

                return parse_core_stats(lines[3 + cpuinstance]);
            }

            /*
             * @comment: Parser helper for ParseFile method
             */
            private static PhysCPU parse_core_stats(string line)
            {
                string[] split_name = new string[64];
                split_name = line.Split(',');

                PhysCPU newCPU = new PhysCPU();

                newCPU.SetActiveTime(Int64.Parse(split_name[2].Trim()));
                newCPU.SetIdleCount(Int64.Parse(split_name[4].Trim()));
                newCPU.SetIdleTime(Int64.Parse(split_name[1].Trim()));
                newCPU.SetTotalTime(Int64.Parse(split_name[3].Trim()));

                return newCPU;
            }

        }

        public class VirtCPU : PhysCPU
        {
            /*
             * Represents the VM that this Virt CPU belongs to. 
             * VCPU instance is represented by base class CoreInstance and not here
             */
            string vm;

            long m_VMEnterCount, m_VMExitCounts;
            long m_LastVMEnterCount, m_LastVMExitCounts;
            long m_Vmmoverhead, m_minVMMoverhead, m_maxVMMoverhead;
            long m_LastVmmoverhead, m_LastMinVMMoverhead, m_LastMaxVMMoverhead;

            public VirtCPU()
                : base()
            {
                m_VMEnterCount = m_VMExitCounts = 0;
                m_LastVMEnterCount = m_LastVMExitCounts = 0;
                m_Vmmoverhead = m_minVMMoverhead = m_maxVMMoverhead = 0;
                m_LastVmmoverhead = m_LastMinVMMoverhead = m_LastMaxVMMoverhead = 0;
            }

            public void Init()
            {
                m_VMEnterCount = m_VMExitCounts = 0;
                m_LastVMEnterCount = m_LastVMExitCounts = 0;
                m_Vmmoverhead = m_minVMMoverhead = m_maxVMMoverhead = 0;
                m_LastVmmoverhead = m_LastMinVMMoverhead = m_LastMaxVMMoverhead = 0;
            }

            public void SetVMName(string name)
            {
                this.vm = name;
            }

            public void SetVMEnterCount(long counter)
            {
                m_LastVMEnterCount = m_VMEnterCount;
                m_VMEnterCount = counter;
            }

            public void SetVMExitCount(long counter)
            {
                m_LastVMExitCounts = m_VMExitCounts;
                m_VMExitCounts = counter;
            }

            public void SetVMMOverhead(long counter)
            {
                m_LastVmmoverhead = m_Vmmoverhead;
                m_Vmmoverhead = counter;
            }

            public void SetMinVMMOverhead(long counter)
            {
                m_LastMinVMMoverhead = m_minVMMoverhead;
                m_minVMMoverhead = counter;
            }

            public void SetMaxVMMOverhead(long counter)
            {
                m_LastMaxVMMoverhead = m_maxVMMoverhead;
                m_maxVMMoverhead = counter;
            }

            public long GetMaxVMMOverhead()
            {
                return m_maxVMMoverhead;
            }

            public long GetMinVMMOverhead()
            {
                return m_minVMMoverhead;
            }

            public long GetVMMOverhead()
            {
                return m_Vmmoverhead - m_LastVmmoverhead;
            }

            public long GetDeltaVMMEnterCount()
            {
                return m_VMEnterCount - m_LastVMEnterCount;
            }

            public long GetDeltaVMMExitCount()
            {
                return m_VMExitCounts - m_LastVMExitCounts;
            }

            public string GetVMName()
            {
                return this.vm;
            }
            /*
             * @comment: Given a file name and a physical CPU instances, parse and
             *           creates a new PhysCPU instance
             * @TODO: Better parse error handling. 
             */
            public static string[] ReadFile(String file)
            {
                string fileName = AppDomain.CurrentDomain.BaseDirectory + file;
                string[] lines = System.IO.File.ReadAllLines(fileName);
                return lines;
            }

            private static int find_virtual_by_name(string[] lines, string name)
            {
                for (int i = 0; i < lines.Count(); i++)
                {
                    if (String.Compare(lines[i].Trim(), name, true) == 0)
                        return i + 1;
                }

                return -1;
            }

            public static VirtCPU ParseFile(string[] lines, string vm, int cpuinstance)
            {

                int lineno = 0;

                lineno = find_virtual_by_name(lines, vm);

                if (lineno < 0)
                    return null;

                if (String.Compare(vm, "linux", true) == 0)
                {
                    lineno += cpuinstance;
                }

                return parse_vcpu_stats(lines[lineno]);

            }

            /*
             * @comment: Parser helper for ParseFile method
             */
            private static VirtCPU parse_vcpu_stats(string line)
            {
                string[] split_name = new string[64];
                split_name = line.Split(',');

                VirtCPU newCPU = new VirtCPU();

                newCPU.SetActiveTime(Int64.Parse(split_name[1].Trim()));
                newCPU.SetIdleCount(Int64.Parse(split_name[6].Trim()));
                newCPU.SetIdleTime(Int64.Parse(split_name[5].Trim()));
                newCPU.SetTotalTime(Int64.Parse(split_name[4].Trim()));
                newCPU.SetVMMOverhead(Int64.Parse(split_name[7].Trim()));
                newCPU.SetVMEnterCount(Int64.Parse(split_name[2].Trim()));
                newCPU.SetVMExitCount(Int64.Parse(split_name[3].Trim()));
                newCPU.SetMinVMMOverhead(Int64.Parse(split_name[8].Trim()));
                newCPU.SetMaxVMMOverhead(Int64.Parse(split_name[9].Trim()));

                return newCPU;
            }

            /*
             * @comment: Subtraction operator overload function 
             */
            public static VirtCPU operator -(VirtCPU lhs, VirtCPU rhs)
            {
                VirtCPU delta = new VirtCPU();
                delta.SetActiveTime(lhs.GetActiveTime() - rhs.GetActiveTime());
                delta.SetIdleCount(lhs.GetIdleCount() - rhs.GetIdleCount());
                delta.SetIdleTime(lhs.GetIdleTime() - rhs.GetIdleTime());
                delta.SetTotalTime(lhs.GetTotalTime() - rhs.GetTotalTime());

                delta.SetCoreInstance(lhs.GetCoreInstance());
                delta.SetVMName(lhs.vm);

                return delta;
            }

        }


        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            tabControl1.Visibility = Visibility.Hidden;
            textBlock2.Text = Platform();
            OSDetect();
            pCPU0IC = new PlotModel { Title = "Physical CPU 0 Idle Request" };
            pCPU1IC = new PlotModel { Title = "Physical CPU 1 Idle Request" };
            pCPU2IC = new PlotModel { Title = "Physical CPU 2 Idle Request" };
            pCPU3IC = new PlotModel { Title = "Physical CPU 3 Idle Request" };
            pCPU0AR = new PlotModel { Title = "Physical CPU 0 Avg. Idle Residency" };
            pCPU1AR = new PlotModel { Title = "Physical CPU 1 Avg. Idle Residency" };
            pCPU2AR = new PlotModel { Title = "Physical CPU 2 Avg. Idle Residency" };
            pCPU3AR = new PlotModel { Title = "Physical CPU 3 Avg. Idle Residency" };
            pCPU0UTIL = new PlotModel { Title = "Physical CPU 0 Utilization" };
            pCPU1UTIL = new PlotModel { Title = "Physical CPU 1 Utilization" };
            pCPU2UTIL = new PlotModel { Title = "Physical CPU 2 Utilization" };
            pCPU3UTIL = new PlotModel { Title = "Physical CPU 3 Utilization" };
            ModemCPUUTIL = new PlotModel { Title = "Modem CPU Utilization" };
            AOSCPUUTIL = new PlotModel { Title = "Android CPU Utilization" };
            vmmOverHead = new PlotModel { Title = "VMM Overhead" };
            VMCS = new PlotModel { Title = "VMM Context Switches" };



            pCpu0 = new PhysCPU();
            pCpu1 = new PhysCPU();
            pCpu2 = new PhysCPU();
            pCpu3 = new PhysCPU();

            Modem = new VirtCPU();
            Linux0 = new VirtCPU();
            Linux1 = new VirtCPU();
            Linux2 = new VirtCPU();
            Linux3 = new VirtCPU();
            //TEE = new VirtCPU();


            deviceUsb = new UsbAdb();
            deviceUsb.Init();
            InitStatus();
            deviceUsb.UpdateConnectionStatus += (s, e) =>
            {
                Dispatcher.Invoke((Action)delegate()
                {
                    UpdateStatusBar((UsbAdb)s);
                });
            };
            CreateProcess_DeviceInfo("ADB_Init.bat");

            /*
             * @todo: Detect the platform information, like SOC type, etc
             *        for now hard code to SoFIA3G
             */
            soc = new SoFIA3G();

            /* 
             * @ comment: Load the interrupt vector mapping
             */
            //soc.LoadVectorDefinition("sofia3g_vectors.xml");

            dsa = new DSA(soc);

           // this.m_UseCaseList = UseCaseProfile.LoadUseCaseProfiles(soc);
            this.m_SelectedUseCase = null;


        }

        public void InitStatus()
        {

            Boolean conn = deviceUsb.CheckSofiaConnected(initconn);
            if (conn == true)
            {
                stsconnection.Text = "SoFIA device connected";
            }
        }
        public void SetTimeStatus(string hours, string minutes)
        {
            TextBlock time = new TextBlock();
            time.Text = DateTime.Now.ToLongTimeString() + "," + DateTime.Now.ToLongDateString();
        }
        public void UpdateStatusBar(UsbAdb device)
        {

            if (device.isConnected("device id string. In case of multiple devices"))
                stsconnection.Text = "SoFIA device connected"; // sbe.statusMessage;
            else
                stsconnection.Text = "SoFIA device disconnected";

            if (device.getDsaState() == (int)UsbAdb.DSAFSM.WAIT_DSLEEP_ENTER)
            {
                dsastatus.Text = "Waiting for Device to enter deep sleep...";
                dsaTextBox.AppendText("Waiting for Device to enter deep sleep...\n\n");
            }

            if (device.getDsaState() == (int)UsbAdb.DSAFSM.IN_DSLEEP)
            {
                dsastatus.Text = "System in deep sleep.";
                dsaTextBox.AppendText("System in deep sleep.\n");
                dsa_enter_timer.Stop();
            }

            if (device.getDsaState() == (int)UsbAdb.DSAFSM.DSLEEP_ENTER_ABORT)
            {
                dsastatus.Text = "System failed to enter deep sleep.";
                dsaTextBox.AppendText("System failed to enter deep sleep.\n");
                stopAnalysis.IsEnabled = false;
                startAnalysis.IsEnabled = true;
            }

            if (device.getDsaState() == (int)UsbAdb.DSAFSM.DSLEEP_DONE)
            {
                dsastatus.Text = "System exited successfully from deep sleep.";
                dsaTextBox.AppendText("System exited successfully from deep sleep.\n");

                stopAnalysis.IsEnabled = false;
                startAnalysis.IsEnabled = true;
                progressbar.Visibility = Visibility.Hidden;
                Remainingtime.Visibility = Visibility.Hidden;

                if (test_wo_device == false)
                {
                    dsa_status_timer.Stop();
                    dsa_wakeup_timer.Stop();
                    dsa_enter_timer.Stop();
                }


                if (deviceUsb.isConnected("blah") && (test_wo_device == false))
                {
                    CreateProcess_DeviceInfo("deepsleepstop.bat");
                }

                ShowDSAResults();
            }

            if (device.getDsaState() == (int)UsbAdb.DSAFSM.DSLEEP_NOT_RECOVERED)
            {
                dsastatus.Text = "System did not recover from deep sleep.";
                dsaTextBox.AppendText("System did not recover from deep sleep.\n");
                stopAnalysis.IsEnabled = false;
                startAnalysis.IsEnabled = true;
            }
        }

        public void OSDetect()
        {
            ANDROID.Visibility = Visibility.Visible;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private DispatcherTimer IntsStatsTimer;

        private void intsTimer_Tick(object sender, EventArgs e)
        {
            CreateProcess_DeviceInfo("get_interrupts.bat");
            Display_Ints("ints");
        }

        private void Interrupts_Click(object sender, RoutedEventArgs e)
        {
            Delete_Items();
            gct.IsEnabled = true;
            tabControl1.Items.Insert(0, gct);
            tabControl1.SelectedItem = gct;
            isQuickLaunched = true;

        }

        private void VMMStats_Click(object sender, RoutedEventArgs e)
        {
            Delete_Items();
            vmmstats.IsEnabled = true;
            tabControl1.Items.Insert(0, vmmstats);
            tabControl1.SelectedItem = vmmstats;
            isQuickLaunched = true;

            stats_stopButton.IsEnabled = false;
            stats_startButton.IsEnabled = true;
            stats_refreshButton.IsEnabled = false;

            SetBackground();
            //vmmStats.Background = Brushes.LightGray;
        }


        private void SetBackground()
        {
            socwatch.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF005693"));

        }

        private void Delete_Items()
        {
            tabControl1.Visibility = Visibility.Visible;

            if (tabControl1.Items.Contains(tabItem0))
            {
                tabControl1.Items.Remove(tabItem0);
            }
            if (tabControl1.Items.Contains(tabItem1))
            {
                tabControl1.Items.Remove(tabItem1);
            }

            if (tabControl1.Items.Contains(gct))
            {
                tabControl1.Items.Remove(gct);
            }

            if (tabControl1.Items.Contains(vmmstats))
            {
                tabControl1.Items.Remove(vmmstats);
            }
            if (tabControl1.Items.Contains(dpa))
            {
                tabControl1.Items.Remove(dpa);
            }
            if (tabControl1.Items.Contains(Setting))
            {
                tabControl1.Items.Remove(Setting);
            }
        }

        private void TabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Minimise_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void Devices_Click(object sender, RoutedEventArgs e)
        {
            Delete_Items();
            tabItem1.IsEnabled = true;
            tabControl1.Items.Insert(0, tabItem1);
            tabControl1.SelectedItem = tabItem1;
            isQuickLaunched = true;
            SetBackground();

            stopButton.IsEnabled = false;
            startButton.IsEnabled = true;
            refreshButton.IsEnabled = false;
        }

        private void Button6_Click_2(object sender, RoutedEventArgs e)
        {
            tabControl1.Visibility = Visibility.Visible;
            tabItem0.Visibility = Visibility.Visible;

            tabItem1.Visibility = Visibility.Visible;
            gct.Visibility = Visibility.Visible;
        }

        public void Display_DevPM(string file)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            string fileName = string.Empty;

            fileName = AppDomain.CurrentDomain.BaseDirectory + @"\\dstates\\" + file + ".txt";
           // string[] line = System.IO.File.ReadAllLines(fileName);
            string[] line = parseDevstates();

           // MessageBox.Show("3"+line.Length);
            string[] lines = Sort(line, sortwith);
            //MessageBox.Show("1");
            //for(int i=4;i<lines.Length;i++){
            //    string[] split_nametosort = new string[1000];

            //    split_nametosort = lines[i].Split(',', ',');
            //    float ontime, offtime, total;
            //    ontime = float.Parse(split_nametosort[2]);

            //   // MessageBox.Show("ontime is:"+ontime);
            ////string[] SortedList = lines.OrderBy<>;
            //    //offtime = float.Parse(split_nametosort[3]);
            //    //o => o.OrderDate).ToList();
            //}


            string[] split_name = new string[1000];
            int index_c = 0;
            int index = 0;
            index = Array.FindIndex(lines, row => row.Contains("Devices state residency"));
            index_c = index + 4;
            p_res_grid.ShowGridLines = false;

            // Define the Columns
            p_res_grid.RowDefinitions.Clear();
            p_res_grid.ColumnDefinitions.Clear();

            ColumnDefinition colDef1 = new ColumnDefinition();
            ColumnDefinition colDef2 = new ColumnDefinition();
            ColumnDefinition colDef3 = new ColumnDefinition();
            ColumnDefinition colDef4 = new ColumnDefinition();
            ColumnDefinition colDef5 = new ColumnDefinition();
            // ColumnDefinition colDef6 = new ColumnDefinition();
            colDef1.Width = new GridLength(200, GridUnitType.Pixel);
            colDef2.Width = new GridLength(100, GridUnitType.Pixel);
            colDef3.Width = new GridLength(250, GridUnitType.Pixel);
            colDef4.Width = new GridLength(250, GridUnitType.Pixel);
            colDef5.Width = new GridLength(200, GridUnitType.Pixel);
            //colDef6.Width = new GridLength(200, GridUnitType.Pixel);
            p_res_grid.ColumnDefinitions.Add(colDef1);
            p_res_grid.ColumnDefinitions.Add(colDef2);
            p_res_grid.ColumnDefinitions.Add(colDef3);
            p_res_grid.ColumnDefinitions.Add(colDef4);
            p_res_grid.ColumnDefinitions.Add(colDef5);
            //p_res_grid.ColumnDefinitions.Add(colDef6);

            for (int z = 0; z < 5; z++)
            {
                TextBlock txt1 = new TextBlock();
                switch (z)
                {
                    case 0:
                        txt1.Text = "Device";
                        break;
                    case 1:
                        txt1.Text = "Power State";
                        break;
                    case 2:
                        txt1.Text = "Ontime (ms) ";
                        break;
                    case 3:
                        txt1.Text = "Offtime(ms)";
                        break;
                    case 4:
                        txt1.Text = "Clock";
                        break;
                    //case 5:
                    //    txt1.Text = "Clock2";
                    //    break;
                }

                txt1.FontSize = 16;
                //txt1.FontWeight = FontWeights.Bold;
                Grid.SetColumnSpan(txt1, 3);
                Grid.SetRow(txt1, 0);
                Grid.SetColumn(txt1, z);
                p_res_grid.Children.Add(txt1);
            }

            for (int j = 1; j <= lines.Length; j++)
            {
                RowDefinition rowDef1 = new RowDefinition();
                rowDef1.MinHeight = 28;
                rowDef1.Height = new GridLength(10, GridUnitType.Star);
                p_res_grid.RowDefinitions.Add(rowDef1);
                string[] split_name3 = new string[100];

                split_name3 = lines[j-1].Split(',', ',');
                float ontime, offtime, total;
                ontime = float.Parse(split_name3[2]);
                offtime = float.Parse(split_name3[3]);
                total = ontime + offtime;
                //MessageBox.Show("4");
                for (int z = 0; z < 5; z++)
                {
                    if ((j % 2) == 1)
                    {
                        SolidColorBrush blueBrush = new SolidColorBrush();
                        blueBrush.Color = Colors.LightBlue;
                        Rectangle blueRectangle = new Rectangle();
                        blueRectangle.Fill = blueBrush;
                        Grid.SetRow(blueRectangle, j);
                        Grid.SetColumn(blueRectangle, z);
                        p_res_grid.Children.Add(blueRectangle);

                    }
                    else
                    {
                        SolidColorBrush AquaBrush = new SolidColorBrush();
                        AquaBrush.Color = Colors.Aqua;
                        Rectangle AquaRectangle = new Rectangle();
                        AquaRectangle.Fill = AquaBrush;
                        Grid.SetRow(AquaRectangle, j);
                        Grid.SetColumn(AquaRectangle, z);
                        p_res_grid.Children.Add(AquaRectangle);
                    }
                    //MessageBox.Show("hi");
                    TextBlock txt1 = new TextBlock();
                    if (z == 2)
                    {
                        txt1.Text = split_name3[z].Trim() + "\n(" + ((ontime / total) * 100).ToString("0.00") + "%)";
                    }
                    else if (z == 3)
                    {
                        txt1.Text = split_name3[z].Trim() + "\n(" + ((offtime / total) * 100).ToString("0.00") + "%)"; ;
                    }
                    else
                        txt1.Text = split_name3[z].Trim();
                  // MessageBox.Show("5");
                    txt1.FontSize = 16;
                    //txt1.FontWeight = FontWeights.Bold;
                    Grid.SetColumnSpan(txt1, 3);
                    Grid.SetRow(txt1, j);
                    Grid.SetColumn(txt1, z);
                    p_res_grid.Children.Add(txt1);
                }
                p_res_grid.Visibility = Visibility.Visible;
            }
        }

        public void DisplayAfterClick(string file, string type)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            string fileName = string.Empty;

            fileName = AppDomain.CurrentDomain.BaseDirectory + @"\\dstates\\" + file + ".txt";
            string[] line = System.IO.File.ReadAllLines(fileName);
            string[] lines = Sort(line, type);

            string[] split_name = new string[1000];
            int index_c = 0;
            int index = 0;
            index = Array.FindIndex(lines, row => row.Contains("Devices state residency"));
            index_c = index + 4;
            p_res_grid.ShowGridLines = false;

            // Define the Columns
            p_res_grid.RowDefinitions.Clear();
            p_res_grid.ColumnDefinitions.Clear();


            ColumnDefinition colDef1 = new ColumnDefinition();
            ColumnDefinition colDef2 = new ColumnDefinition();
            ColumnDefinition colDef3 = new ColumnDefinition();
            ColumnDefinition colDef4 = new ColumnDefinition();
            ColumnDefinition colDef5 = new ColumnDefinition();
            // ColumnDefinition colDef6 = new ColumnDefinition();
            colDef1.Width = new GridLength(200, GridUnitType.Pixel);
            colDef2.Width = new GridLength(100, GridUnitType.Pixel);
            colDef3.Width = new GridLength(250, GridUnitType.Pixel);
            colDef4.Width = new GridLength(250, GridUnitType.Pixel);
            colDef5.Width = new GridLength(200, GridUnitType.Pixel);
            //colDef6.Width = new GridLength(200, GridUnitType.Pixel);
            p_res_grid.ColumnDefinitions.Add(colDef1);
            p_res_grid.ColumnDefinitions.Add(colDef2);
            p_res_grid.ColumnDefinitions.Add(colDef3);
            p_res_grid.ColumnDefinitions.Add(colDef4);
            p_res_grid.ColumnDefinitions.Add(colDef5);
            //p_res_grid.ColumnDefinitions.Add(colDef6);


            for (int z = 0; z < 5; z++)
            {
                TextBlock txt1 = new TextBlock();
                switch (z)
                {
                    case 0:
                        txt1.Text = "Device";
                        break;
                    case 1:
                        txt1.Text = "Power State";
                        break;
                    case 2:
                        txt1.Text = "Ontime (ms) ";
                        break;
                    case 3:
                        txt1.Text = "Offtime(ms)";
                        break;
                    case 4:
                        txt1.Text = "Clock1";
                        break;
                    //case 5:
                    //    txt1.Text = "Clock2";
                    //    break;
                }

                txt1.FontSize = 16;
                //txt1.FontWeight = FontWeights.Bold;
                Grid.SetColumnSpan(txt1, 3);
                Grid.SetRow(txt1, 0);
                Grid.SetColumn(txt1, z);
                p_res_grid.Children.Add(txt1);
            }



            for (int j = 4; j <= lines.Length - 3; j++)
            {
                RowDefinition rowDef1 = new RowDefinition();
                rowDef1.MinHeight = 28;
                rowDef1.Height = new GridLength(10, GridUnitType.Star);
                p_res_grid.RowDefinitions.Add(rowDef1);
                string[] split_name3 = new string[100];

                split_name3 = lines[j].Split(',', ',');
                float ontime, offtime, total;
                ontime = float.Parse(split_name3[2]);
                offtime = float.Parse(split_name3[3]);
                total = ontime + offtime;

                for (int z = 0; z < 5; z++)
                {
                    if ((j % 2) == 1)
                    {
                        SolidColorBrush blueBrush = new SolidColorBrush();
                        blueBrush.Color = Colors.LightBlue;
                        Rectangle blueRectangle = new Rectangle();
                        blueRectangle.Fill = blueBrush;
                        Grid.SetRow(blueRectangle, j - 3);
                        Grid.SetColumn(blueRectangle, z);
                        p_res_grid.Children.Add(blueRectangle);
                    }
                    else
                    {
                        SolidColorBrush AquaBrush = new SolidColorBrush();
                        AquaBrush.Color = Colors.Aqua;
                        Rectangle AquaRectangle = new Rectangle();
                        AquaRectangle.Fill = AquaBrush;
                        Grid.SetRow(AquaRectangle, j - 3);
                        Grid.SetColumn(AquaRectangle, z);
                        p_res_grid.Children.Add(AquaRectangle);

                    }
                    TextBlock txt1 = new TextBlock();

                    if (z == 2)
                    {
                        txt1.Text = split_name3[z].Trim() + "\n(" + ((ontime / total) * 100).ToString("0.00") + "%)";
                    }
                    else if (z == 3)
                    {
                        txt1.Text = split_name3[z].Trim() + "\n(" + ((offtime / total) * 100).ToString("0.00") + "%)"; ;
                    }
                    else
                        txt1.Text = split_name3[z].Trim();
                    txt1.FontSize = 16;
                    //txt1.FontWeight = FontWeights.Bold;
                    Grid.SetColumnSpan(txt1, 3);
                    Grid.SetRow(txt1, j - 3);
                    Grid.SetColumn(txt1, z);
                    p_res_grid.Children.Add(txt1);
                   //                   MessageBox.Show("data is" + p_res_grid.Children.ToString());
                }
                p_res_grid.Visibility = Visibility.Visible;
            }
        }

        public void Display_Ints(string file)
        {
            string count = GetCount();
            int cpu = Int32.Parse(count);
            int n = (cpu * 2) + 1;
            int num = cpu;
           // string[] prelines = IRQ.ReadFile("dstates\\" + file + ".txt");
            string[] prelines = parseInterrupts(cpu);
            List<IRQ> irqStats = new List<IRQ>();
          //  MessageBox.Show("Cpu count is----:"+this.soc);
            irqStats = IRQ.ParseFile(prelines, this.soc);
            int_list_grid.ShowGridLines = false;

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;

            // Define the Columns
            int_list_grid.RowDefinitions.Clear();
            int_list_grid.ColumnDefinitions.Clear();

            for (int i = 1; i <= n; i++)
            {
                if (cpu > 3)
                {
                    int_list_grid.Width = 1500;
                    if (i == 1 || i == cpu + 1)
                    {
                        ColumnDefinition colDef = new ColumnDefinition();
                        colDef.Width = new GridLength(2, GridUnitType.Star);
                        int_list_grid.ColumnDefinitions.Add(colDef);
                    }
                    else
                    {
                        ColumnDefinition colDef = new ColumnDefinition();
                        colDef.Width = new GridLength(2, GridUnitType.Star);
                        int_list_grid.ColumnDefinitions.Add(colDef);
                    }
                }
                else
                {
                    ColumnDefinition colDef = new ColumnDefinition();
                    colDef.Width = new GridLength(2, GridUnitType.Star);
                    int_list_grid.ColumnDefinitions.Add(colDef);
                }
                TextBlock txt = new TextBlock();
                txt.Text = "IRQ name";
                txt.FontSize = 16;
                //txt1.FontWeight = FontWeights.Bold;
                Grid.SetColumnSpan(txt, 3);
                Grid.SetRow(txt, 0);
                Grid.SetColumn(txt, 0);
                int_list_grid.Children.Add(txt);
               // MessageBox.Show("1");
                for (int z = 1; z <n; z++)
                {
                    if (z<=cpu)
                    {
                        TextBlock txt2 = new TextBlock();
                        txt2.Text = "Count(CPU" + (z - 1).ToString() + ")";
                        txt2.FontSize = 16;
                        //txt1.FontWeight = FontWeights.Bold;
                        Grid.SetColumnSpan(txt2, 3);
                        Grid.SetRow(txt2, 0);
                        Grid.SetColumn(txt2, z);
                        int_list_grid.Children.Add(txt2);
                    }
                    else
                    {
                        TextBlock txt3 = new TextBlock();
                       // MessageBox.Show("number is-----:" + (cpu - 1));
                        txt3.Text = "Wakecount(CPU" +  (z -(cpu+1)).ToString() + ")";
                        txt3.FontSize = 16;
                        //txt1.FontWeight = FontWeights.Bold;
                       // MessageBox.Show("z value----:"+z);
                        Grid.SetColumnSpan(txt3, 3);
                        Grid.SetRow(txt3, 0);
                        Grid.SetColumn(txt3, z);
                        int_list_grid.Children.Add(txt3);
                      //  MessageBox.Show("3");
                       //cpu--;
                    }
                }

                //for (int k = n - 1; k > cpu; k--)
                //{
                //    TextBlock txt3 = new TextBlock();
                //    MessageBox.Show("number is-----:" +(cpu - 1));
                //    txt3.Text = "Wakecount(CPU" + (n-(cpu - 1)).ToString() + ")";
                //    txt3.FontSize = 16;
                //    //txt1.FontWeight = FontWeights.Bold;
                //    Grid.SetColumnSpan(txt3, 3);
                //    Grid.SetRow(txt3, 0);
                //    Grid.SetColumn(txt3, k);
                //    int_list_grid.Children.Add(txt3);
                //    cpu--;
                //   // MessageBox.Show("nuber is----:" + num );
                //}


                int j = 2;
                
                foreach (IRQ irq in irqStats)
                {
                    RowDefinition rowDef1 = new RowDefinition();
                    rowDef1.MinHeight = 28;
                    rowDef1.Height = new GridLength(10, GridUnitType.Star);
                    int_list_grid.RowDefinitions.Add(rowDef1);
                    for (int z = 0; z < n; z++)
                    {
                        if ((j % 2) == 1)
                        {
                            SolidColorBrush blueBrush = new SolidColorBrush();
                            blueBrush.Color = Colors.LightBlue;
                            Rectangle blueRectangle = new Rectangle();
                            blueRectangle.Fill = blueBrush;
                            Grid.SetRow(blueRectangle, j - 1);
                            Grid.SetColumn(blueRectangle, z);
                            int_list_grid.Children.Add(blueRectangle);
                        }

                        else
                        {
                            SolidColorBrush AquaBrush = new SolidColorBrush();
                            AquaBrush.Color = Colors.Aqua;
                            Rectangle AquaRectangle = new Rectangle();
                            AquaRectangle.Fill = AquaBrush;
                            Grid.SetRow(AquaRectangle, j - 1);
                            Grid.SetColumn(AquaRectangle, z);
                            int_list_grid.Children.Add(AquaRectangle);
                        }

                        if (z == 0)
                        {
                            TextBlock txt1 = new TextBlock();
                            txt1.Text = irq.GetIrqName();
                            txt1.FontSize = 16;
                            //txt1.FontWeight = FontWeights.Bold;
                            Grid.SetColumnSpan(txt1, 3);
                            Grid.SetRow(txt1, j - 1);
                            Grid.SetColumn(txt1, z);
                            int_list_grid.Children.Add(txt1);
                            // MessageBox.Show("done");
                        }
                    }

                    for (int m = 1; m < cpu + 1; m++)
                    {
                        TextBlock txtcpu = new TextBlock();
                        txtcpu.Text = irq.GetInterruptCount(m - 1).ToString("N");
                        txtcpu.FontSize = 16;
                        //txt1.FontWeight = FontWeights.Bold;
                        Grid.SetColumnSpan(txtcpu, 3);
                        Grid.SetRow(txtcpu, j - 1);
                        Grid.SetColumn(txtcpu, m);
                        int_list_grid.Children.Add(txtcpu);
                    }

                    for (int k = n - 1; k > cpu; k--)
                    {
                        TextBlock txtwake = new TextBlock();
                        txtwake.Text = irq.GetWakeInterruptCount(k - (cpu + 1)).ToString("N");
                        txtwake.FontSize = 16;
                        //txt1.FontWeight = FontWeights.Bold;
                        Grid.SetColumnSpan(txtwake, 3);
                        Grid.SetRow(txtwake, j - 1);
                        Grid.SetColumn(txtwake, k);
                        int_list_grid.Children.Add(txtwake);
                        num--;
                    }

                    //if(z==1)
                    //{
                    //    TextBlock txtcpu = new TextBlock();
                    //    for (int m = z; m <= cpu; m++)
                    //    {
                    //        txtcpu.Text = irq.GetInterruptCount(m - 1).ToString("N");
                    //        txtcpu.FontSize = 16;
                    //        //txt1.FontWeight = FontWeights.Bold;
                    //        Grid.SetColumnSpan(txtcpu, 3);
                    //        Grid.SetRow(txtcpu, j - 1);
                    //        Grid.SetColumn(txtcpu, m);
                    //        int_list_grid.Children.Add(txtcpu);
                    //    }
                    //}

                    //if(z==cpu+1)
                    //{
                    //    MessageBox.Show("entered");
                    //    TextBlock txtmodem = new TextBlock();
                    //    for (z = cpu + 1; z <= n; z++)
                    //    {
                    //        txtmodem.Text = irq.GetWakeInterruptCount(z - (cpu + 1)).ToString("N");
                    //        txtmodem.FontSize = 16;
                    //        //txt1.FontWeight = FontWeights.Bold;
                    //        Grid.SetColumnSpan(txtmodem, 3);
                    //        Grid.SetRow(txtmodem, j - 1);
                    //        Grid.SetColumn(txtmodem, z);
                    //        int_list_grid.Children.Add(txtmodem);
                    //    }
                    //}

                    //switch (z)
                    //{
                    //    case 0:
                    //        txt1.Text = irq.GetIrqName();
                    //        break;
                    //    case 1:
                    //    case 2:
                    //        txt1.Text = irq.GetInterruptCount(z - 1).ToString("N");
                    //        break;
                    //    case 3:
                    //    case 4:
                    //        txt1.Text = irq.GetWakeInterruptCount(z - 3).ToString("N");
                    //        break;
                    //    default:                       
                    //        break;
                    //}

                    int_list_grid.Visibility = Visibility.Visible;
                    j++;
                }
            }
        }

        private string[] parsevmmstats(string line)
        {
            string[] ret_array = new string[14];
            string[] split_name = new string[64];
            long c0time, idletime, vmmoverhead, tottime;
            double c0timeper = 0, idletimeper = 0, vmmoverheadper = 0;
            split_name = line.Split(',');
            tottime = Int64.Parse(split_name[5].Trim());
            c0time = Int64.Parse(split_name[2].Trim());
            idletime = Int64.Parse(split_name[6].Trim());
            vmmoverhead = Int64.Parse(split_name[8].Trim());

            if (tottime > 0)
            {
                c0timeper = (c0time * 100) / tottime;
                idletimeper = (idletime * 100) / tottime;
                vmmoverheadper = (vmmoverhead * 100) / tottime;
            }
           
            //ret_array[0] = split_name[0].Trim();
            //ret_array[1] = split_name[1].Trim();
            ret_array[0] = c0timeper.ToString("0.00") + "%";
            ret_array[1] = idletimeper.ToString("0.00") + "%";
            ret_array[2] = vmmoverheadper.ToString("0.00") + "%";
            ret_array[3] = split_name[2].Trim(); // Active time
            ret_array[4] = split_name[6].Trim(); // Idle time
            ret_array[5] = split_name[8].Trim(); // VMM overhead
            ret_array[6] = split_name[3].Trim(); // VM enter count
            ret_array[7] = split_name[4].Trim(); // VM exit count
            ret_array[8] = split_name[11].Trim(); // Exit idle count. unused
            ret_array[9] = split_name[7].Trim(); // Idle count
            ret_array[10] = split_name[9].Trim(); // Min VMM overhead
            ret_array[11] = split_name[10].Trim(); // Max VMM overhead
            //
            //ss
            //
            ret_array[12] = tottime.ToString();
            ret_array[13] = split_name[0].Trim();
           // MessageBox.Show(""+ret_array[13]);
            return ret_array;

        }

        private int find_virtual_by_name(string[] lines, string name)
        {
            for (int i = 0; i < lines.Count(); i++)
            {
                if (String.Compare(lines[i].Trim(), name, true) == 0)
                    return i + 1;
            }
            return -1;
        }

        public void display_vmmstats(string file)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            string fileName = string.Empty;
            string[] vcpu_stats = { "Time(Active) %", 
                                    "Time (Idle) %",
                                    "VMM overhead %", 
                                    "Time(Active) ms",
                                    "Time (Idle) ms",
                                    "VMM overhead ms", 
                                    "VM Entry Count", 
                                    "VM Exit Count",
                                    "VM Exit(interrupt) Count",
                                    "Idle Enter Count",
                                  };

            fileName = AppDomain.CurrentDomain.BaseDirectory + @"\\dstates\\" + file + ".txt";
            string[] lines = parseVirtualstats();
            //MessageBox.Show(""+lines[0]);
            //MessageBox.Show("1");

            if (lines.Length <= 1)
            {
                return;
            }

            string count = GetCount();
            //MessageBox.Show("2          "+count);
            int cpu = Int32.Parse(count);
            int n = cpu + 2;
           // MessageBox.Show("2");
            vmmstats_list_grid.ShowGridLines = false;

            // Define the Columns
            vmmstats_list_grid.RowDefinitions.Clear();
            vmmstats_list_grid.ColumnDefinitions.Clear();
           // MessageBox.Show("cleared");
            for (int i = 1; i <= n; i++)
            {
                ColumnDefinition colDef = new ColumnDefinition();
                colDef.Width = new GridLength(150, GridUnitType.Pixel);
                vmmstats_list_grid.ColumnDefinitions.Add(colDef);
            }
            vmmstats_list_grid.Width = Vcpuheader.Width = (cpu * 150) + 300;
            //ColumnDefinition colDef1 = new ColumnDefinition();
            //ColumnDefinition colDef2 = new ColumnDefinition();
            //ColumnDefinition colDef3 = new ColumnDefinition();
            //ColumnDefinition colDef4 = new ColumnDefinition();
            ////ColumnDefinition colDef5 = new ColumnDefinition();
            //colDef1.Width = new GridLength(150, GridUnitType.Pixel);
            //colDef2.Width = new GridLength(150, GridUnitType.Pixel);
            //colDef3.Width = new GridLength(150, GridUnitType.Pixel);
            //colDef4.Width = new GridLength(150, GridUnitType.Pixel);
            //vmmstats_list_grid.ColumnDefinitions.Add(colDef1);
            //vmmstats_list_grid.ColumnDefinitions.Add(colDef2);
            //vmmstats_list_grid.ColumnDefinitions.Add(colDef3);
            //vmmstats_list_grid.ColumnDefinitions.Add(colDef4);
            //vmmstats_list_grid.ColumnDefinitions.Add(colDef5);

            TextBlock txt = new TextBlock();
            txt.Text = "Metrics";
            txt.FontSize = 14;
            //txt1.FontWeight = FontWeights.Bold;
            Grid.SetColumnSpan(txt, 3);
            Grid.SetRow(txt, 0);
            Grid.SetColumn(txt, 0);
            vmmstats_list_grid.Children.Add(txt);

            TextBlock txt3 = new TextBlock();
            txt3.Text = "Modem";
            txt3.FontSize = 14;
            //txt1.FontWeight = FontWeights.Bold;
            Grid.SetColumnSpan(txt3, 3);
            Grid.SetRow(txt3, 0);
            Grid.SetColumn(txt3, 1);
            vmmstats_list_grid.Children.Add(txt3);

            for (int z = 2; z < n; z++)
            {
                TextBlock txt2 = new TextBlock();
                txt2.Text = "Android(VCPU" + (z - 2).ToString() + ")";
                txt2.FontSize = 16;
                //txt1.FontWeight = FontWeights.Bold;
                Grid.SetColumnSpan(txt2, 3);
                Grid.SetRow(txt2, 0);
                Grid.SetColumn(txt2, z);
                vmmstats_list_grid.Children.Add(txt2);
            }

          //  MessageBox.Show("3");
            //for (int z = 0; z < 4; z++)
            //{
            //    TextBlock txt1 = new TextBlock();
            //    switch (z)
            //    {
            //        case 0:
            //            txt1.Text = "Metrics";
            //            break;
            //        case 1:
            //            txt1.Text = "Modem";
            //            break;
            //        case 2:
            //            txt1.Text = "Android(VCPU0)";
            //            break;
            //        case 3:
            //            txt1.Text = "Android(VCPU1)";
            //            break;
            //        case 4:
            //            txt1.Text = "TEE";
            //            break;
            //    }

            //    txt1.FontSize = 14;
            //    //txt1.FontWeight = FontWeights.Bold;
            //    Grid.SetColumnSpan(txt1, 3);
            //    Grid.SetRow(txt1, 0);
            //    Grid.SetColumn(txt1, z);
            //    vmmstats_list_grid.Children.Add(txt1);
            //}

            int mexlineno=0, linuxlineno=1 ;
            string[] modem_vmmstats_info = new string[32];
            string[] and0_vmmstats_info = new string[32];
            string[] and1_vmmstats_info = new string[52];
            string[] and2_vmmstats_info = new string[52];
            string[] and3_vmmstats_info = new string[52];
            string[] tee_vmmstats_info = new string[52];

            // MessageBox.Show("4");

            for(int i=0;i<lines.Length;i++)
            {
                string[] data = parsevmmstats(lines[i]);
                //MessageBox.Show("5");
                if (data[13] == "mex")
                {
                    mexlineno = i;
                }
                if (data[13] == "linux")
                {
                    linuxlineno = i;
                }                
            }

             //MessageBox.Show(""+linuxlineno);
            // int lineno = 0;

           //lineno = find_virtual_by_name(lines, "mex");

            //if (lineno > 0)
                modem_vmmstats_info = parsevmmstats(lines[mexlineno]);
           // lineno = find_virtual_by_name(lines, "linux");

                //MessageBox.Show("line numeber"+linuxlineno);
                if (cpu == 2)
                {
                    and0_vmmstats_info = parsevmmstats(lines[linuxlineno - 1]);
                    and1_vmmstats_info = parsevmmstats(lines[linuxlineno]);
                }
                if (cpu == 4)
                {
                    and0_vmmstats_info = parsevmmstats(lines[linuxlineno - 3]);
                    and1_vmmstats_info = parsevmmstats(lines[linuxlineno - 2]);
                    and2_vmmstats_info = parsevmmstats(lines[linuxlineno - 1]);
                    and3_vmmstats_info = parsevmmstats(lines[linuxlineno]);
                }
            
            //    MessageBox.Show("1");

            //tee_vmmstats_info = parsevmmstats(lines[9]);
            Modem.SetActiveTime(Int64.Parse(modem_vmmstats_info[3]));
            Modem.SetIdleTime(Int64.Parse(modem_vmmstats_info[4]));
            Modem.SetTotalTime(Int64.Parse(modem_vmmstats_info[12]));
            Modem.SetIdleCount(Int64.Parse(modem_vmmstats_info[9]));
            Modem.SetVMEnterCount(Int64.Parse(modem_vmmstats_info[6]));
            Modem.SetVMExitCount(Int64.Parse(modem_vmmstats_info[7]));
            Modem.SetVMMOverhead(Int64.Parse(modem_vmmstats_info[5]));
            Modem.SetMinVMMOverhead(Int64.Parse(modem_vmmstats_info[10]));
            Modem.SetMaxVMMOverhead(Int64.Parse(modem_vmmstats_info[11]));

            if (cpu == 2)
            {
                Linux0.SetActiveTime(Int64.Parse(and0_vmmstats_info[3]));
                Linux0.SetIdleTime(Int64.Parse(and0_vmmstats_info[4]));
                Linux0.SetTotalTime(Int64.Parse(and0_vmmstats_info[12]));
                Linux0.SetIdleCount(Int64.Parse(and0_vmmstats_info[9]));
                Linux0.SetVMEnterCount(Int64.Parse(and0_vmmstats_info[6]));
                Linux0.SetVMExitCount(Int64.Parse(and0_vmmstats_info[7]));
                Linux0.SetVMMOverhead(Int64.Parse(and0_vmmstats_info[5]));
                Linux0.SetMinVMMOverhead(Int64.Parse(and0_vmmstats_info[10]));
                Linux0.SetMaxVMMOverhead(Int64.Parse(and0_vmmstats_info[11]));

                Linux1.SetActiveTime(Int64.Parse(and1_vmmstats_info[3]));
                Linux1.SetIdleTime(Int64.Parse(and1_vmmstats_info[4]));
                Linux1.SetTotalTime(Int64.Parse(and1_vmmstats_info[12]));
                Linux1.SetIdleCount(Int64.Parse(and1_vmmstats_info[9]));
                Linux1.SetVMEnterCount(Int64.Parse(and1_vmmstats_info[6]));
                Linux1.SetVMExitCount(Int64.Parse(and1_vmmstats_info[7]));
                Linux1.SetVMMOverhead(Int64.Parse(and1_vmmstats_info[5]));
                Linux1.SetMinVMMOverhead(Int64.Parse(and1_vmmstats_info[10]));
                Linux1.SetMaxVMMOverhead(Int64.Parse(and1_vmmstats_info[11]));
            }
            if (cpu == 4)
            {
                Linux0.SetActiveTime(Int64.Parse(and0_vmmstats_info[3]));
                Linux0.SetIdleTime(Int64.Parse(and0_vmmstats_info[4]));
                Linux0.SetTotalTime(Int64.Parse(and0_vmmstats_info[12]));
                Linux0.SetIdleCount(Int64.Parse(and0_vmmstats_info[9]));
                Linux0.SetVMEnterCount(Int64.Parse(and0_vmmstats_info[6]));
                Linux0.SetVMExitCount(Int64.Parse(and0_vmmstats_info[7]));
                Linux0.SetVMMOverhead(Int64.Parse(and0_vmmstats_info[5]));
                Linux0.SetMinVMMOverhead(Int64.Parse(and0_vmmstats_info[10]));
                Linux0.SetMaxVMMOverhead(Int64.Parse(and0_vmmstats_info[11]));

                Linux1.SetActiveTime(Int64.Parse(and1_vmmstats_info[3]));
                Linux1.SetIdleTime(Int64.Parse(and1_vmmstats_info[4]));
                Linux1.SetTotalTime(Int64.Parse(and1_vmmstats_info[12]));
                Linux1.SetIdleCount(Int64.Parse(and1_vmmstats_info[9]));
                Linux1.SetVMEnterCount(Int64.Parse(and1_vmmstats_info[6]));
                Linux1.SetVMExitCount(Int64.Parse(and1_vmmstats_info[7]));
                Linux1.SetVMMOverhead(Int64.Parse(and1_vmmstats_info[5]));
                Linux1.SetMinVMMOverhead(Int64.Parse(and1_vmmstats_info[10]));
                Linux1.SetMaxVMMOverhead(Int64.Parse(and1_vmmstats_info[11]));

                Linux2.SetActiveTime(Int64.Parse(and2_vmmstats_info[3]));
                Linux2.SetIdleTime(Int64.Parse(and2_vmmstats_info[4]));
                Linux2.SetTotalTime(Int64.Parse(and2_vmmstats_info[12]));
                Linux2.SetIdleCount(Int64.Parse(and2_vmmstats_info[9]));
                Linux2.SetVMEnterCount(Int64.Parse(and2_vmmstats_info[6]));
                Linux2.SetVMExitCount(Int64.Parse(and2_vmmstats_info[7]));
                Linux2.SetVMMOverhead(Int64.Parse(and2_vmmstats_info[5]));
                Linux2.SetMinVMMOverhead(Int64.Parse(and2_vmmstats_info[10]));
                Linux2.SetMaxVMMOverhead(Int64.Parse(and2_vmmstats_info[11]));

                Linux3.SetActiveTime(Int64.Parse(and3_vmmstats_info[3]));
                Linux3.SetIdleTime(Int64.Parse(and3_vmmstats_info[4]));
                Linux3.SetTotalTime(Int64.Parse(and3_vmmstats_info[12]));
                Linux3.SetIdleCount(Int64.Parse(and3_vmmstats_info[9]));
                Linux3.SetVMEnterCount(Int64.Parse(and3_vmmstats_info[6]));
                Linux3.SetVMExitCount(Int64.Parse(and3_vmmstats_info[7]));
                Linux3.SetVMMOverhead(Int64.Parse(and3_vmmstats_info[5]));
                Linux3.SetMinVMMOverhead(Int64.Parse(and3_vmmstats_info[10]));
                Linux3.SetMaxVMMOverhead(Int64.Parse(and3_vmmstats_info[11]));
            }
            
            for (int j = 0; j <9; j++)
            {
                RowDefinition rowDef1 = new RowDefinition();
                rowDef1.MinHeight = 28;
                rowDef1.Height = new GridLength(1, GridUnitType.Star);
                vmmstats_list_grid.RowDefinitions.Add(rowDef1);

                for (int z = 0; z < n; z++)
                {
                    if ((j % 2) == 1)
                    {
                        SolidColorBrush blueBrush = new SolidColorBrush();
                        blueBrush.Color = Colors.LightBlue;
                        Rectangle blueRectangle = new Rectangle();
                        blueRectangle.Fill = blueBrush;
                        Grid.SetRow(blueRectangle, j + 1);
                        Grid.SetColumn(blueRectangle, z);
                        vmmstats_list_grid.Children.Add(blueRectangle);
                    }
                    else
                    {
                        SolidColorBrush AquaBrush = new SolidColorBrush();
                        AquaBrush.Color = Colors.Aqua;
                        Rectangle AquaRectangle = new Rectangle();
                        AquaRectangle.Fill = AquaBrush;
                        Grid.SetRow(AquaRectangle, j + 1);
                        Grid.SetColumn(AquaRectangle, z);
                        vmmstats_list_grid.Children.Add(AquaRectangle);
                    }
                    TextBlock txt1 = new TextBlock();
                    switch (z)
                    {
                        case 0:
                          
                            txt1.Text = vcpu_stats[j].Trim();
                            break;
                        case 1:
                            txt1.Text = modem_vmmstats_info[j].Trim();
                            break;
                        case 2:
                            txt1.Text = and0_vmmstats_info[j].Trim();
                            break;
                        case 3:
                            txt1.Text = and1_vmmstats_info[j].Trim();
                            break;
                        case 4:
                            txt1.Text = and2_vmmstats_info[j].Trim();
                            break;
                        case 5:
                            txt1.Text = and3_vmmstats_info[j].Trim();
                            break;
                    }
                    txt1.FontSize = 16;
                    //txt1.FontWeight = FontWeights.Bold;
                    Grid.SetColumnSpan(txt1, 3);
                    Grid.SetRow(txt1, j + 1);
                    Grid.SetColumn(txt1, z);
                    vmmstats_list_grid.Children.Add(txt1);
                }
                vmmstats_list_grid.Visibility = Visibility.Visible;
            }
        }

        private string[] ParseVMM_ia_Stats(string line)
        {
            string[] ret_array = new string[10];
            string[] split_name = new string[64];
            long c0time, idletime, tottime;
            double c0timeper, idletimeper;
            split_name = line.Split(',');

            tottime = Int64.Parse(split_name[3].Trim());
            c0time = Int64.Parse(split_name[2].Trim());
            idletime = Int64.Parse(split_name[1].Trim());

           // MessageBox.Show("hey");

            if (tottime > 0)
            {
                c0timeper = (c0time * 100) / tottime;
                idletimeper = (idletime * 100) / tottime;
            }
            else
            {
                c0timeper = 0;
                idletimeper = 0;
            }

            //MessageBox.Show("hey1");

            ret_array[0] = c0timeper.ToString("0.00") + "%";
            ret_array[1] = idletimeper.ToString("0.00") + "%";
            ret_array[2] = split_name[2].Trim();
            ret_array[3] = split_name[1].Trim();
            ret_array[4] = split_name[4].Trim();
            //
            // Used for plotting
            //
            ret_array[5] = tottime.ToString();
            ret_array[6] = c0timeper.ToString("0");
           // MessageBox.Show("hey2");
            return ret_array;
        }

        public void Display_VMM_ia_Stats(string file)
        {
            string count = GetCount();
            int cpu = Int32.Parse(count);
            int n = cpu + 1;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            string fileName = string.Empty;

            string[] pcpu_stats = { "Time(Active) %", 
                                    "Time (Idle) %",
                                    "Time(Active) ms",
                                    "Time (Idle) ms",
                                    "Idle Enter Count"};

            fileName = AppDomain.CurrentDomain.BaseDirectory + @"\\dstates\\" + file + ".txt";
           // string[] lines = System.IO.File.ReadAllLines(fileName);
            string[] lines = parsePhysicalstats();

            vmm_iacore_list_grid.ShowGridLines = false;

            // Define the Columns
            vmm_iacore_list_grid.RowDefinitions.Clear();
            vmm_iacore_list_grid.ColumnDefinitions.Clear();


            for (int i = 1; i <= n; i++)
            {
                if (cpu == 4)
                {
                    Pcpuheader.Width = vmm_iacore_list_grid.Width = ((cpu - 1) * 100) + 150;
                }
                
                if (i == 0)
                {
                    ColumnDefinition colDef = new ColumnDefinition();
                    colDef.Width = new GridLength(150, GridUnitType.Pixel);
                    vmm_iacore_list_grid.ColumnDefinitions.Add(colDef);
                }
                else
                {
                    ColumnDefinition colDef = new ColumnDefinition();
                    colDef.Width = new GridLength(100, GridUnitType.Pixel);
                    vmm_iacore_list_grid.ColumnDefinitions.Add(colDef);
                }
            }

            TextBlock txt3 = new TextBlock();
            txt3.Text = "Metrics";
            txt3.FontSize = 14;
            //txt1.FontWeight = FontWeights.Bold;
            Grid.SetColumnSpan(txt3, 3);
            Grid.SetRow(txt3, 0);
            Grid.SetColumn(txt3, 0);
            vmm_iacore_list_grid.Children.Add(txt3);

            for (int z = 1; z < cpu + 1; z++)
            {
                TextBlock txt2 = new TextBlock();
                txt2.Text = "CPU" + (z - 1).ToString();
                txt2.FontSize = 16;
                //txt1.FontWeight = FontWeights.Bold;
                Grid.SetColumnSpan(txt2, 3);
                Grid.SetRow(txt2, 0);
                Grid.SetColumn(txt2, z);
                vmm_iacore_list_grid.Children.Add(txt2);
            }

            string[] cpu0_vmmstats_info = new string[32];
            string[] cpu1_vmmstats_info = new string[32];
            string[] cpu2_vmmstats_info = new string[32];
            string[] cpu3_vmmstats_info = new string[32];

            //MessageBox.Show("2");
            if (lines.Length >= 1)
            {
                cpu0_vmmstats_info = ParseVMM_ia_Stats(lines[0]);
                pCpu0.SetActiveTime(Int64.Parse(cpu0_vmmstats_info[2]));
                pCpu0.SetIdleTime(Int64.Parse(cpu0_vmmstats_info[3]));
                pCpu0.SetTotalTime(Int64.Parse(cpu0_vmmstats_info[5]));
                pCpu0.SetIdleCount(Int64.Parse(cpu0_vmmstats_info[4]));
            }

            if (lines.Length >= 2)
            {
                cpu1_vmmstats_info = ParseVMM_ia_Stats(lines[1]);
                pCpu1.SetActiveTime(Int64.Parse(cpu1_vmmstats_info[2]));
                pCpu1.SetIdleTime(Int64.Parse(cpu1_vmmstats_info[3]));
                pCpu1.SetTotalTime(Int64.Parse(cpu1_vmmstats_info[5]));
                pCpu1.SetIdleCount(Int64.Parse(cpu1_vmmstats_info[4]));
               
            }

            if (lines.Length >= 3)
            {
                cpu2_vmmstats_info = ParseVMM_ia_Stats(lines[2]);
                pCpu2.SetActiveTime(Int64.Parse(cpu2_vmmstats_info[2]));
                pCpu2.SetIdleTime(Int64.Parse(cpu2_vmmstats_info[3]));
                pCpu2.SetTotalTime(Int64.Parse(cpu2_vmmstats_info[5]));
                pCpu2.SetIdleCount(Int64.Parse(cpu2_vmmstats_info[4]));
                
            }

            if (lines.Length >= 4)
            {
                cpu3_vmmstats_info = ParseVMM_ia_Stats(lines[3]);
                pCpu3.SetActiveTime(Int64.Parse(cpu3_vmmstats_info[2]));
                pCpu3.SetIdleTime(Int64.Parse(cpu3_vmmstats_info[3]));
                pCpu3.SetTotalTime(Int64.Parse(cpu3_vmmstats_info[5]));
                pCpu3.SetIdleCount(Int64.Parse(cpu3_vmmstats_info[4]));
            }

            for (int j = 0; j <5; j++)
            {
                RowDefinition rowDef1 = new RowDefinition();
                rowDef1.MinHeight = 28;
                rowDef1.Height = new GridLength(1, GridUnitType.Star);
                vmm_iacore_list_grid.RowDefinitions.Add(rowDef1);

                for (int z = 0; z < n; z++)
                {
                    if ((j % 2) == 1)
                    {
                        SolidColorBrush blueBrush = new SolidColorBrush();
                        blueBrush.Color = Colors.LightBlue;
                        Rectangle blueRectangle = new Rectangle();
                        blueRectangle.Fill = blueBrush;
                        Grid.SetRow(blueRectangle, j + 1);
                        Grid.SetColumn(blueRectangle, z);
                        vmm_iacore_list_grid.Children.Add(blueRectangle);
                    }
                    else
                    {
                        SolidColorBrush AquaBrush = new SolidColorBrush();
                        AquaBrush.Color = Colors.Aqua;
                        Rectangle AquaRectangle = new Rectangle();
                        AquaRectangle.Fill = AquaBrush;

                        Grid.SetRow(AquaRectangle, j + 1);
                        Grid.SetColumn(AquaRectangle, z);
                        vmm_iacore_list_grid.Children.Add(AquaRectangle);

                    }

                    TextBlock txt1 = new TextBlock();

                    switch (z)
                    {
                        case 0:
                            txt1.Text = pcpu_stats[j].Trim();
                            break;
                        case 1:
                            txt1.Text = cpu0_vmmstats_info[j].Trim();
                            break;
                        case 2:
                            txt1.Text = cpu1_vmmstats_info[j].Trim();
                            break;
                        case 3:                        
                            txt1.Text = cpu2_vmmstats_info[j].Trim();
                            break;
                        case 4:
                            txt1.Text = cpu3_vmmstats_info[j].Trim();
                            break;
                    }

                    txt1.FontSize = 16;
                    //txt1.FontWeight = FontWeights.Bold;
                    Grid.SetColumnSpan(txt1, 3);
                    Grid.SetRow(txt1, j + 1);
                    Grid.SetColumn(txt1, z);
                    vmm_iacore_list_grid.Children.Add(txt1);
                }
                vmm_iacore_list_grid.Visibility = Visibility.Visible;
            }
        }

        public void Display_VMM_Charts()
        {
            int p0_time = (int)pCpu0.GetTotalTime() / 1000;
            int p0_ic = 0;
            int p1_time = (int)pCpu1.GetTotalTime() / 1000;
            int p1_ic = 0;
            int p2_time = (int)pCpu2.GetTotalTime() / 1000;
            int p2_ic = 0;
            int p3_time = (int)pCpu3.GetTotalTime() / 1000;
            int p3_ic = 0;
            int p0_util = 0, p1_util = 0, p2_util = 0, p3_util = 0;
            int mcpu_vmcs_cnt = 0, acpu0_vmcs_cnt = 0, acpu1_vmcs_cnt = 0, acpu2_vmcs_cnt = 0, acpu3_vmcs_cnt = 0;

            int modem_total = (int)Modem.GetTotalTime() / 1000;
            int modem_util = 0, acpu0_util = 0, acpu1_util = 0, acpu2_util = 0, acpu3_util = 0;

            int vmm_overhead = 0, vmm_minoverhead = 0, vmm_maxoverhead = 0;

            if (pCpu0.GetElapsedTime() > 0)
            {
                p0_util = ((int)pCpu0.GetActiveTime() * 100) / (int)pCpu0.GetElapsedTime();
                p0_ic = (int)((pCpu0.GetIdleCount() * 1000) / pCpu0.GetElapsedTime());
            }

            if (pCpu1.GetElapsedTime() > 0)
            {
                p1_util = ((int)pCpu1.GetActiveTime() * 100) / (int)pCpu1.GetElapsedTime();
                p1_ic = (int)((pCpu1.GetIdleCount() * 1000) / pCpu1.GetElapsedTime());
            }

            if (pCpu2.GetElapsedTime() > 0)
            {
                p2_util = ((int)pCpu2.GetActiveTime() * 100) / (int)pCpu2.GetElapsedTime();
                p2_ic = (int)((pCpu2.GetIdleCount() * 1000) / pCpu2.GetElapsedTime());
            }

            if (pCpu3.GetElapsedTime() > 0)
            {
                p3_util = ((int)pCpu3.GetActiveTime() * 100) / (int)pCpu3.GetElapsedTime();
                p3_ic = (int)((pCpu3.GetIdleCount() * 1000) / pCpu3.GetElapsedTime());
            }

            if (Modem.GetElapsedTime() > 0)
            {
                modem_util = ((int)Modem.GetActiveTime() * 100) / (int)pCpu0.GetElapsedTime();
                mcpu_vmcs_cnt = (int)((Modem.GetDeltaVMMEnterCount() * 1000) / Modem.GetElapsedTime());
            }

            if (Linux0.GetElapsedTime() > 0)
            {
                acpu0_util = ((int)Linux0.GetActiveTime() * 100) / (int)Linux0.GetElapsedTime();
                acpu0_vmcs_cnt = (int)((Linux0.GetDeltaVMMEnterCount() * 1000) / Linux0.GetElapsedTime());
            }

            if (Linux1.GetElapsedTime() > 0)
            {
                acpu1_util = ((int)Linux1.GetActiveTime() * 100) / (int)Linux1.GetElapsedTime();
                acpu1_vmcs_cnt = (int)((Linux1.GetDeltaVMMEnterCount() * 1000) / Linux1.GetElapsedTime());
            }

            if (Modem.GetDeltaVMMEnterCount() > 0)
                vmm_overhead = ((int)Modem.GetVMMOverhead() * 1000) / (int)Modem.GetDeltaVMMEnterCount();

            vmm_minoverhead = (int)Modem.GetMinVMMOverhead();
            vmm_maxoverhead = (int)Modem.GetMaxVMMOverhead();

            pcpu0_ic_Series.Points.Add(new DataPoint(p0_time, p0_ic));
            pcpu1_ic_Series.Points.Add(new DataPoint(p1_time, p1_ic));
            pcpu2_ic_Series.Points.Add(new DataPoint(p2_time, p2_ic));
            pcpu3_ic_Series.Points.Add(new DataPoint(p3_time, p3_ic));

            pcpu0_avgres_Series.Points.Add(new DataPoint(p0_time, pCpu0.GetAverageIdleResidency()));
            pcpu1_avgres_Series.Points.Add(new DataPoint(p1_time, pCpu1.GetAverageIdleResidency()));
            pcpu2_avgres_Series.Points.Add(new DataPoint(p2_time, pCpu2.GetAverageIdleResidency()));
            pcpu3_avgres_Series.Points.Add(new DataPoint(p3_time, pCpu3.GetAverageIdleResidency()));

            pcpu0_util_Series.Points.Add(new DataPoint(p0_time, p0_util));
            pcpu1_util_Series.Points.Add(new DataPoint(p1_time, p1_util));
            pcpu2_util_Series.Points.Add(new DataPoint(p2_time, p2_util));
            pcpu3_util_Series.Points.Add(new DataPoint(p3_time, p3_util));

            mcpu_util_Series.Points.Add(new DataPoint(modem_total, modem_util));
            acpu0_util_Series.Points.Add(new DataPoint(modem_total, acpu0_util));
            acpu1_util_Series.Points.Add(new DataPoint(modem_total, acpu1_util));

            //vmm_overhead_Series.Points.Add(new DataPoint(modem_total, vmm_overhead));
            vmm_minoverhead_Series.Points.Add(new DataPoint(modem_total, vmm_minoverhead));
            vmm_maxoverhead_Series.Points.Add(new DataPoint(modem_total, vmm_maxoverhead));

            vmcs_m_Series.Points.Add(new DataPoint(modem_total, mcpu_vmcs_cnt));
            vmcs_acpu0_Series.Points.Add(new DataPoint(Linux0.GetTotalTime() / 1000, acpu0_vmcs_cnt));
            vmcs_acpu1_Series.Points.Add(new DataPoint(Linux1.GetTotalTime() / 1000, acpu1_vmcs_cnt));

            pCPU0IC.InvalidatePlot(true);
            pCPU1IC.InvalidatePlot(true);
            pCPU2IC.InvalidatePlot(true);
            pCPU3IC.InvalidatePlot(true);

            pCPU0AR.InvalidatePlot(true);
            pCPU1AR.InvalidatePlot(true);
            pCPU2AR.InvalidatePlot(true);
            pCPU3AR.InvalidatePlot(true);

            pCPU0UTIL.InvalidatePlot(true);
            pCPU1UTIL.InvalidatePlot(true);
            pCPU2UTIL.InvalidatePlot(true);
            pCPU3UTIL.InvalidatePlot(true);

            ModemCPUUTIL.InvalidatePlot(true);
            AOSCPUUTIL.InvalidatePlot(true);
            VMCS.InvalidatePlot(true);
            vmmOverHead.InvalidatePlot(true);
        }


        private void Add_Items()
        {
            tabControl1.Items.Insert(0, tabItem0);
            tabControl1.Items.Insert(1, tabItem1);
            tabControl1.Items.Insert(3, gct);
            tabControl1.Items.Insert(4, dpa);
            tabControl1.Items.Insert(5, Setting);
            gct.IsEnabled = false;
            tabItem1.IsEnabled = false;
            dpa.IsEnabled = false;

        }

        public string Platform()
        {
            string platformtype = "";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from " + "Win32_Processor");
            try
            {
                foreach (ManagementObject share in searcher.Get())
                {
                    try
                    {
                        string intelcheck = share["Manufacturer"].ToString();
                        if (string.Compare(intelcheck, "GenuineIntel") == 0)
                        {
                            Genuine = true;

                        }
                        else
                        {
                            Genuine = false;
                        }

                        string PlatformCheck = share["Description"].ToString();
                        //MessageBox.Show(PlatformCheck);
                        string model = (UInt32.Parse(PlatformCheck.Substring(PlatformCheck.IndexOf("Model") + 5 + 1, 2)).ToString("X"));


                        if (Int32.Parse(PlatformCheck.Substring(PlatformCheck.IndexOf("Family") + 6 + 1, 1)) == 6 && UInt32.Parse(model, System.Globalization.NumberStyles.HexNumber) == 0x37)
                        {
                            platformtype = "BYT";
                        }
                        else if (Int32.Parse(PlatformCheck.Substring(PlatformCheck.IndexOf("Family") + 6 + 1, 1)) == 6 && UInt32.Parse(model, System.Globalization.NumberStyles.HexNumber) == 0x35)
                        {
                            platformtype = "CLV";
                        }
                        else
                        {
                            platformtype = "SoFIA";
                        }
                    }
                    catch
                    {
                        platformtype = "";
                    }
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("can't get data because of the followeing error \n" + exp.Message, "Error");
                platformtype = "";
            }

            return platformtype;
        }


        public void CreateProcess_DeviceInfo(string procname)
        {
            DeviceInfo = string.Empty;
            count = 0;
            Process Build = new Process();
            Build.StartInfo.Verb = "run as";
            Build.StartInfo.CreateNoWindow = true;
            Build.StartInfo.FileName = procname;
            Build.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Build.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Build.StartInfo.RedirectStandardError = true;
            Build.StartInfo.RedirectStandardOutput = true;
            Build.StartInfo.UseShellExecute = false;
            Build.EnableRaisingEvents = true;
            Build.OutputDataReceived += new DataReceivedEventHandler(OnDataReceived1);
           // Build.Start();
            //Build.BeginOutputReadLine();
            /*Build.BeginErrorReadLine();
            Build.WaitForExit();
            Build.Close();*/
        }

        public void CreateProcess_DeviceInfo_WithArgs(string procname, string arguments)
        {
            DeviceInfo = string.Empty;
            count = 0;
            Process Build = new Process();
            Build.StartInfo.Verb = "run as";
            Build.StartInfo.CreateNoWindow = true;
            Build.StartInfo.FileName = procname;
            Build.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Build.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Build.StartInfo.RedirectStandardError = true;
            Build.StartInfo.RedirectStandardOutput = true;
            Build.StartInfo.UseShellExecute = false;
            Build.StartInfo.Arguments = arguments;
            Build.EnableRaisingEvents = true;
            Build.OutputDataReceived += new DataReceivedEventHandler(OnDataReceived1);
            Build.Start();
            Build.BeginOutputReadLine();
            Build.BeginErrorReadLine();
            Build.WaitForExit();
            Build.Close();
        }

        public void DisableUSB()
        {
            ProcessStartInfo psi = new ProcessStartInfo(AppDomain.CurrentDomain.BaseDirectory + @"/devcon64.exe");
            psi.Arguments = @"remove @USB*";
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.Verb = "runas";
            Process.Start(psi);
            UsbDisable = true;
            stopAnalysis.IsEnabled = true;
            startAnalysis.IsEnabled = false;
        }
        public void EnableUSB()
        {
            ProcessStartInfo PSI = new ProcessStartInfo(AppDomain.CurrentDomain.BaseDirectory + @"/devcon64.exe");
            PSI.Arguments = @"/r rescan";
            PSI.WindowStyle = ProcessWindowStyle.Hidden;
            PSI.Verb = "runas";
            Process.Start(PSI);
            UsbDisable = false;
            startAnalysis.IsEnabled = true;
            stopAnalysis.IsEnabled = false;
        }

        public void OnDataReceived1(object sender, DataReceivedEventArgs e)
        {
            count++;

            if (e.Data != null)
            {


                if (count == 7)
                {
                    DeviceInfo = e.Data;
                }
            }
        }

        private void Minimise_Click_1(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {

            if (System.IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\\trace.txt"))
            {
                System.IO.File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"\\trace.txt");
            }
            if (System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\\start.cmd"))
            {
                System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\\start.cmd");
            }


            try
            {
                foreach (Process process in Process.GetProcesses())
                {
                    if (process.ProcessName.Contains("GPIO") || process.ProcessName.Contains("wpa") || process.ProcessName.Contains("ACPI") || process.ProcessName.Contains("PnP"))
                        process.Kill();
                }
            }
            catch
            {

            }

            this.Close();
        }

        private void Maximize_Click_1(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.MaxHeight = System.Windows.SystemParameters.WorkArea.Height + 10;
                this.MaxWidth = System.Windows.SystemParameters.WorkArea.Width + 10;
                this.WindowState = WindowState.Maximized;

            }
            else
            {
                this.WindowState = WindowState.Normal;
            }

        }

        private DispatcherTimer DevpmStatsTimer;

        private void devpmTimer_Tick(object sender, EventArgs e)
        {
            /* call Adb_devices.bat and get device states */
            CreateProcess_DeviceInfo("devicepmstart.bat");

            Display_DevPM("dstates");
        }
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Boolean conn = deviceUsb.isConnected("blah");
            if (conn == true)
            {
                stopButton.IsEnabled = true;
                startButton.IsEnabled = false;
                refreshButton.IsEnabled = true;

                /* call Adb_devices.bat and get device states */
                CreateProcess_DeviceInfo("devicepmstart.bat");
                Display_DevPM("dstates");
                devPmRefreshCombo.IsEnabled = false;

                DevpmStatsTimer = new System.Windows.Threading.DispatcherTimer();

                if (!dsref_dis.IsSelected)
                {
                    int refresh_time = 2;

                    if (dsref_3sec.IsSelected)
                        refresh_time = 3;

                    if (dsref_5sec.IsSelected)
                        refresh_time = 5;

                    DevpmStatsTimer.Tick += new EventHandler(devpmTimer_Tick);
                    DevpmStatsTimer.Interval = new TimeSpan(0, 0, refresh_time);
                    DevpmStatsTimer.Start();
                    refreshButton.IsEnabled = false;

                }
                else
                {
                    DevpmStatsTimer.IsEnabled = false;
                    refreshButton.IsEnabled = true;
                }
            }
            else
            {
                if (MessageBox.Show("SoFIA device is not connected.Do you want to continue?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    stopButton.IsEnabled = true;
                    startButton.IsEnabled = false;
                    refreshButton.IsEnabled = true;

                    /* call Adb_devices.bat and get device states */
                    CreateProcess_DeviceInfo("devicepmstart.bat");
                    Display_DevPM("dstates");
                    devPmRefreshCombo.IsEnabled = false;

                    DevpmStatsTimer = new System.Windows.Threading.DispatcherTimer();

                    if (!dsref_dis.IsSelected)
                    {
                        int refresh_time = 2;

                        if (dsref_3sec.IsSelected)
                            refresh_time = 3;

                        if (dsref_5sec.IsSelected)
                            refresh_time = 5;

                        DevpmStatsTimer.Tick += new EventHandler(devpmTimer_Tick);
                        DevpmStatsTimer.Interval = new TimeSpan(0, 0, refresh_time);
                        DevpmStatsTimer.Start();
                        refreshButton.IsEnabled = false;

                    }
                    else
                    {
                        DevpmStatsTimer.IsEnabled = false;
                        refreshButton.IsEnabled = true;
                    }
                }
                else
                {

                }
            }

        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (DevpmStatsTimer.IsEnabled == true)
                DevpmStatsTimer.Stop();

            stopButton.IsEnabled = false;
            startButton.IsEnabled = true;
            refreshButton.IsEnabled = false;

            devPmRefreshCombo.IsEnabled = true;

            CreateProcess_DeviceInfo("devicepmstop.bat");
            if (!Log_toCsv.IsEnabled)
            {
                WriteToCsv("dstates", DstatesCount, "dstates");
                DstatesCount++;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            stopButton.IsEnabled = true;
            startButton.IsEnabled = false;
            refreshButton.IsEnabled = true;
            CreateProcess_DeviceInfo("devicepmrefresh.bat");

            /*read the dstates and populate table in display1 function */
            Display_DevPM("dstates");
            if (!Log_toCsv.IsEnabled)
            {
                WriteToCsv("dstates", DstatesCount, "dstates");
                DstatesCount++;
            }
        }

        private void Int_RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            CreateProcess_DeviceInfo("get_interrupts.bat");

            /*read the interrupts and populate table */
            Display_Ints("ints");
        }

        private DispatcherTimer vmmStatsTimer;

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            /* call Adb_devices.bat and get device states */
            CreateProcess_DeviceInfo("vmmstats_start.bat");

            display_vmmstats("vmmstats");
            Display_VMM_ia_Stats("cpustats");
            Display_VMM_Charts();
        }

        public void InitGraphSeries()
        {

            pcpu0_ic_Series = new LineSeries() { LineLegendPosition = LineLegendPosition.End };
            pcpu1_ic_Series = new LineSeries();
            pcpu2_ic_Series = new LineSeries();
            pcpu3_ic_Series = new LineSeries();

            pcpu0_avgres_Series = new LineSeries();
            pcpu1_avgres_Series = new LineSeries();
            pcpu2_avgres_Series = new LineSeries();
            pcpu3_avgres_Series = new LineSeries();

            pcpu0_util_Series = new LineSeries();
            pcpu1_util_Series = new LineSeries();
            pcpu2_util_Series = new LineSeries();
            pcpu3_util_Series = new LineSeries();

            mcpu_util_Series = new LineSeries();
            acpu0_util_Series = new LineSeries("VCPU0");
            acpu1_util_Series = new LineSeries("VCPU1");
            vmm_overhead_Series = new LineSeries();
            vmm_minoverhead_Series = new LineSeries("Min");
            vmm_maxoverhead_Series = new LineSeries("Max");
            vmcs_m_Series = new LineSeries("Modem VCPU0");
            vmcs_acpu0_Series = new LineSeries("Android VCPU0");
            vmcs_acpu1_Series = new LineSeries("Android VCPU1");

            pcpu0_ic_Series.Smooth = true;
            pcpu1_ic_Series.Smooth = true;
            pcpu2_ic_Series.Smooth = true;
            pcpu3_ic_Series.Smooth = true;

            pcpu0_avgres_Series.Smooth = true;
            pcpu1_avgres_Series.Smooth = true;
            pcpu2_avgres_Series.Smooth = true;
            pcpu3_avgres_Series.Smooth = true;

            pcpu0_util_Series.Smooth = true;
            pcpu1_util_Series.Smooth = true;
            pcpu2_util_Series.Smooth = true;
            pcpu3_util_Series.Smooth = true;

            mcpu_util_Series.Smooth = true;
            acpu0_util_Series.Smooth = true;
            acpu1_util_Series.Smooth = true;
            vmm_overhead_Series.Smooth = true;
            vmm_minoverhead_Series.Smooth = true;
            vmm_maxoverhead_Series.Smooth = true;

            vmcs_m_Series.Smooth = true;
            vmcs_acpu0_Series.Smooth = true;
            vmcs_acpu1_Series.Smooth = true;

            pCPU0IC.Series.Add(pcpu0_ic_Series);
            pCPU1IC.Series.Add(pcpu1_ic_Series);
            pCPU2IC.Series.Add(pcpu2_ic_Series);
            pCPU3IC.Series.Add(pcpu3_ic_Series);

            pCPU0IC.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Idle Count") { AxislineStyle = LineStyle.Solid });
            pCPU0IC.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

            pCPU1IC.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Idle Count") { AxislineStyle = LineStyle.Solid });
            pCPU1IC.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

            pCPU2IC.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Idle Count") { AxislineStyle = LineStyle.Solid });
            pCPU2IC.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

            pCPU3IC.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Idle Count") { AxislineStyle = LineStyle.Solid });
            pCPU3IC.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

            pCPU0AR.Series.Add(pcpu0_avgres_Series);
            pCPU1AR.Series.Add(pcpu1_avgres_Series);
            pCPU2AR.Series.Add(pcpu2_avgres_Series);
            pCPU3AR.Series.Add(pcpu3_avgres_Series);

            pCPU0AR.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Residency (us)") { AxislineStyle = LineStyle.Solid });
            pCPU0AR.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });
            pCPU1AR.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Residency (us)") { AxislineStyle = LineStyle.Solid });
            pCPU1AR.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

            pCPU2AR.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Residency (us)") { AxislineStyle = LineStyle.Solid });
            pCPU2AR.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });
            pCPU3AR.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Residency (us)") { AxislineStyle = LineStyle.Solid });
            pCPU3AR.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

            pCPU0UTIL.Series.Add(pcpu0_util_Series);
            pCPU1UTIL.Series.Add(pcpu1_util_Series);
            pCPU2UTIL.Series.Add(pcpu2_util_Series);
            pCPU3UTIL.Series.Add(pcpu3_util_Series);

            pCPU0UTIL.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Util (%)") { AxislineStyle = LineStyle.Solid });
            pCPU0UTIL.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

            pCPU1UTIL.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Util (%)") { AxislineStyle = LineStyle.Solid });
            pCPU1UTIL.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

            ModemCPUUTIL.Series.Add(mcpu_util_Series);
            ModemCPUUTIL.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Util (%)") { AxislineStyle = LineStyle.Solid });
            ModemCPUUTIL.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

            AOSCPUUTIL.Series.Add(acpu0_util_Series);
            AOSCPUUTIL.Series.Add(acpu1_util_Series);
            AOSCPUUTIL.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Util (%)") { AxislineStyle = LineStyle.Solid });
            AOSCPUUTIL.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

            VMCS.Series.Add(vmcs_m_Series);
            VMCS.Series.Add(vmcs_acpu0_Series);
            VMCS.Series.Add(vmcs_acpu1_Series);
            VMCS.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Context Switches(%)") { AxislineStyle = LineStyle.Solid });
            VMCS.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

            //vmmOverHead.Series.Add(vmm_overhead_Series);
            vmmOverHead.Series.Add(vmm_minoverhead_Series);
            vmmOverHead.Series.Add(vmm_maxoverhead_Series);
            vmmOverHead.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Left, "Overhead Per Switch(us)") { AxislineStyle = LineStyle.Solid });
            vmmOverHead.Axes.Add(new OxyPlot.Axes.LinearAxis(OxyPlot.Axes.AxisPosition.Bottom, "Time (sec)") { AxislineStyle = LineStyle.Solid });

        }

        private void VMM_StartButton_Click(object sender, RoutedEventArgs e)
        {
           
            //if (Idlerequest.IsChecked == true)
            //{
            //    MessageBox.Show("count:" + stackpanel.Children.Count);
            //    MessageBox.Show("" + stackpanel4.Children.Count);
            //    //stackpanel.Children.RemoveAt(stackpanel.Children.Count - 1);
            //    // stackpanel.Children.RemoveAt(stackpanel4.Children.Count-1);
            //    stackpanel.Children.Clear();
            //    MessageBox.Show("deleted");
            //    //stackpanel.Children.Insert();
            //    stackpanel.Children.Add(stat_grid1);
            //    MessageBox.Show("one deleted");
            //    stackpanel.Children.Add(stat_grid2);
            //    // stackpanel.Children.Add(stat_grid5);
            //}
            int cpu = Int32.Parse(GetCount());
            Boolean conn = deviceUsb.isConnected("blah");
            if (conn == true)
            {
                pCpu0.Init();
                pCpu1.Init();
                pCpu2.Init();
                pCpu3.Init();
                Modem.Init();
                Linux0.Init();
                Linux1.Init();

                InitGraphSeries();

                stats_stopButton.IsEnabled = true;
                stats_startButton.IsEnabled = false;
                stats_refreshButton.IsEnabled = true;

                vmmStatsTimer = new System.Windows.Threading.DispatcherTimer();

                if (!vmmrf_dis.IsSelected)
                {
                    int refresh_time = 2;

                    if (vmmrf_3sec.IsSelected)
                        refresh_time = 3;

                    if (vmmrf_5sec.IsSelected)
                        refresh_time = 5;

                    vmmStatsTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                    vmmStatsTimer.Interval = new TimeSpan(0, 0, refresh_time);
                    vmmStatsTimer.Start();
                    stats_refreshButton.IsEnabled = false;

                }
                else
                {
                    vmmStatsTimer.IsEnabled = false;
                    stats_refreshButton.IsEnabled = true;
                }


                vmmRefreshCombo.IsEnabled = false;


                /* call Adb_devices.bat and get device states */
                CreateProcess_DeviceInfo("vmmstats_start.bat");

                /*read the dstates and populate table in display1 function */
                display_vmmstats("vmmstats");
               //               MessageBox.Show("done");
                Display_VMM_ia_Stats("cpustats");
                

                stackpanel.Children.Clear();
                if (cpu == 2)
                {
                    if (Idlerequest.IsChecked == true)
                    {
                        Gridcontrol2 gridobj = new Gridcontrol2();
                        stackpanel.Children.Add(gridobj);
                    }
                    if (Idleresidency.IsChecked == true)
                    {
                        GridcontrolAR2 gridar = new GridcontrolAR2();
                        stackpanel.Children.Add(gridar);
                    }
                    if (pcpuutilization.IsChecked == true)
                    {
                        GridcontrolUTIL2 gridutil = new GridcontrolUTIL2();
                        stackpanel.Children.Add(gridutil);
                    }
                }
                else
                {
                    stackpanel.Children.Clear();
                    if (Idlerequest.IsChecked == true)
                    {
                        GridControl gridobj = new GridControl();
                        stackpanel.Children.Add(gridobj);
                    }
                    if (Idleresidency.IsChecked == true)
                    {
                        GridcontrolAR gridar = new GridcontrolAR();
                        stackpanel.Children.Add(gridar);
                    }
                    if (pcpuutilization.IsChecked == true)
                    {
                        GridcontrolUTIL gridutil = new GridcontrolUTIL();
                        stackpanel.Children.Add(gridutil);
                    }
                    
                }
                if (modemcpuutil.IsChecked == true)
                {
                    Modemcpuutil gridmodem = new Modemcpuutil();
                    stackpanel.Children.Add(gridmodem);
                }
                if (androidcpuutil.IsChecked == true)
                {
                    AOSCpuutil aos = new AOSCpuutil();
                    stackpanel.Children.Add(aos);
                }
                if (vmmoverhead.IsChecked == true)
                {
                    VmmOverhead overhead = new VmmOverhead();
                    stackpanel.Children.Add(overhead);
                }
                if (vmmcontextswitch.IsChecked == true)
                {
                    VMCS vmcs = new VMCS();
                    stackpanel.Children.Add(vmcs);
                }


                Display_VMM_Charts();
                
              //  MessageBox.Show("done");
            }
            else
            {
                if (MessageBox.Show("SoFIA device is not connected.Do you want to continue?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    pCpu0.Init();
                    pCpu1.Init();
                    Modem.Init();
                    Linux0.Init();
                    Linux1.Init();

                    InitGraphSeries();

                    stats_stopButton.IsEnabled = true;
                    stats_startButton.IsEnabled = false;
                    stats_refreshButton.IsEnabled = true;

                    vmmStatsTimer = new System.Windows.Threading.DispatcherTimer();

                    if (!vmmrf_dis.IsSelected)
                    {
                        int refresh_time = 2;

                        if (vmmrf_3sec.IsSelected)
                            refresh_time = 3;

                        if (vmmrf_5sec.IsSelected)
                            refresh_time = 5;

                        vmmStatsTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                        vmmStatsTimer.Interval = new TimeSpan(0, 0, refresh_time);
                        vmmStatsTimer.Start();
                        stats_refreshButton.IsEnabled = false;

                    }
                    else
                    {
                        vmmStatsTimer.IsEnabled = false;
                        stats_refreshButton.IsEnabled = true;
                    }


                    vmmRefreshCombo.IsEnabled = false;


                    /* call Adb_devices.bat and get device states */
                    CreateProcess_DeviceInfo("vmmstats_start.bat");

                    /*read the dstates and populate table in display1 function */
                    display_vmmstats("vmmstats");
                    //MessageBox.Show("done");
                    Display_VMM_ia_Stats("cpustats");
                    stackpanel.Children.Clear();
                    if (cpu == 2)
                    {
                        if (Idlerequest.IsChecked == true)
                        {
                            Gridcontrol2 gridobj = new Gridcontrol2();
                            stackpanel.Children.Add(gridobj);
                        }
                        if (Idleresidency.IsChecked == true)
                        {
                            GridcontrolAR2 gridar = new GridcontrolAR2();
                            stackpanel.Children.Add(gridar);
                        }
                        if (pcpuutilization.IsChecked == true)
                        {
                            GridcontrolUTIL2 gridutil = new GridcontrolUTIL2();
                            stackpanel.Children.Add(gridutil);
                        }
                    }
                    else
                    {
                        if (Idlerequest.IsChecked == true)
                        {
                            GridControl gridobj = new GridControl();
                            stackpanel.Children.Add(gridobj);
                        }
                        if (Idleresidency.IsChecked == true)
                        {
                            GridcontrolAR gridar = new GridcontrolAR();
                            stackpanel.Children.Add(gridar);
                        }
                        if (pcpuutilization.IsChecked == true)
                        {
                            GridcontrolUTIL gridutil = new GridcontrolUTIL();
                            stackpanel.Children.Add(gridutil);
                        }
                    }
                    if (modemcpuutil.IsChecked == true)
                    {
                        Modemcpuutil gridmodem = new Modemcpuutil();
                        stackpanel.Children.Add(gridmodem);
                    }
                    if (androidcpuutil.IsChecked == true)
                    {
                        AOSCpuutil aos = new AOSCpuutil();
                        stackpanel.Children.Add(aos);
                    }
                    if (vmmoverhead.IsChecked == true)
                    {
                        VmmOverhead overhead = new VmmOverhead();
                        stackpanel.Children.Add(overhead);
                    }
                    if (vmmcontextswitch.IsChecked == true)
                    {
                        VMCS vmcs = new VMCS();
                        stackpanel.Children.Add(vmcs);
                    }
                   

                    Display_VMM_Charts();

                }
                else
                {

                }
            }
            display();
            Idlerequest.IsEnabled = false;
            Idleresidency.IsEnabled = false;
            pcpuutilization.IsEnabled = false;
            modemcpuutil.IsEnabled = false;
            androidcpuutil.IsEnabled = false;
            vmmoverhead.IsEnabled = false;
            vmmcontextswitch.IsEnabled = false;

        }

        public void display()
        {
            if(Idlerequest.IsChecked==true)
            {
                GridControl control = new GridControl();
                control.stat_grid1.Visibility = Visibility.Visible;
                control.stat_grid2.Visibility = Visibility.Visible;

            }
        }
        private void VMM_StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (vmmStatsTimer.IsEnabled == true)
                vmmStatsTimer.Stop();

            stats_stopButton.IsEnabled = false;
            stats_startButton.IsEnabled = true;
            stats_refreshButton.IsEnabled = false;
            vmmRefreshCombo.IsEnabled = true;
            CreateProcess_DeviceInfo("vmmstats_stop.bat");
            pCPU0IC.Series.Remove(pcpu0_ic_Series);
            pCPU1IC.Series.Remove(pcpu1_ic_Series);
            pCPU2IC.Series.Remove(pcpu2_ic_Series);
            pCPU3IC.Series.Remove(pcpu3_ic_Series);
            pCPU0IC.Axes.Clear();
            pCPU1IC.Axes.Clear();
            pCPU2IC.Axes.Clear();
            pCPU3IC.Axes.Clear();

            pCPU0AR.Series.Remove(pcpu0_avgres_Series);
            pCPU1AR.Series.Remove(pcpu1_avgres_Series);
            pCPU2AR.Series.Remove(pcpu2_avgres_Series);
            pCPU3AR.Series.Remove(pcpu3_avgres_Series);

            pCPU0AR.Axes.Clear();
            pCPU1AR.Axes.Clear();
            pCPU2AR.Axes.Clear();
            pCPU3AR.Axes.Clear();


            pCPU0UTIL.Series.Remove(pcpu0_util_Series);
            pCPU1UTIL.Series.Remove(pcpu1_util_Series);
            pCPU2UTIL.Series.Remove(pcpu2_util_Series);
            pCPU3UTIL.Series.Remove(pcpu3_util_Series);
            pCPU0UTIL.Axes.Clear();
            pCPU1UTIL.Axes.Clear();
            pCPU2UTIL.Axes.Clear();
            pCPU3UTIL.Axes.Clear();

            ModemCPUUTIL.Series.Remove(mcpu_util_Series);
            //vmmOverHead.Series.Remove(vmm_overhead_Series);

            AOSCPUUTIL.Series.Remove(acpu0_util_Series);
            AOSCPUUTIL.Series.Remove(acpu1_util_Series);

            vmmOverHead.Series.Remove(vmm_minoverhead_Series);
            vmmOverHead.Series.Remove(vmm_maxoverhead_Series);

            VMCS.Series.Remove(vmcs_m_Series);
            VMCS.Series.Remove(vmcs_acpu0_Series);
            VMCS.Series.Remove(vmcs_acpu1_Series);

            ModemCPUUTIL.Axes.Clear();
            vmmOverHead.Axes.Clear();
            AOSCPUUTIL.Axes.Clear();
            VMCS.Axes.Clear();
            if (!Log_toCsv.IsEnabled)
            {
                WriteToCsv("vmmstats", VmmStatCount, "vmmstats");
                WriteToCsv("cpustats", VmmStatCount, "cpustats");
                VmmStatCount++;
            }
            Idlerequest.IsEnabled = true;
            Idleresidency.IsEnabled = true;
            pcpuutilization.IsEnabled = true;
            modemcpuutil.IsEnabled = true;
            androidcpuutil.IsEnabled = true;
            vmmoverhead.IsEnabled = true;
            vmmcontextswitch.IsEnabled = true;
            
            

        }

        private void VMM_RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            CreateProcess_DeviceInfo("vmmstats_refresh.bat");

            /*read the interrupts and populate table */
            display_vmmstats("vmmstats");
           // MessageBox.Show("Done");
            Display_VMM_ia_Stats("cpustats");

            // this line has to be after the display_vmmstats and display_vmm_ia_stats
            Display_VMM_Charts();
            if (!Log_toCsv.IsEnabled)
            {
                WriteToCsv("vmmstats", VmmStatCount, "vmmstats");
                WriteToCsv("cpustats", VmmStatCount, "vmmstats");
                VmmStatCount++;
            }
        }
        void WriteToCsv(string file, int Count, String type)
        {

            string fileName = string.Empty;
            fileName = AppDomain.CurrentDomain.BaseDirectory + @"\\dstates\\" + file + ".txt";
            string[] lines = System.IO.File.ReadAllLines(fileName);
            //string[] lines = Sort(line);
            String path = logsavepath + "\\" + file + ".csv";


            if (type == "dstates")
            {
                if (Count == 0)
                {
                    using (CsvFileWriter writer = new CsvFileWriter(path))
                    {
                        // String header = lines[2].Replace(" ",",");
                        string header = "Device,State,on_time(milli sec),off_time(milli sec),clock";

                        writer.WriteLine(header);

                        for (int i = 4; i < lines.Length; i++)
                        {
                            CsvRow row = new CsvRow();
                            row.Add(lines[i]);
                            StringBuilder builder = new StringBuilder();

                            bool firstColumn = true;
                            foreach (string value in row)
                            {
                                builder.Append(value);
                                firstColumn = false;
                            }
                            row.LineText = builder.ToString();
                            writer.WriteLine(row.LineText);
                        }
                        writer.Flush();
                        writer.Close();
                    }
                }

                else
                {
                    using (StreamWriter writer = File.AppendText(path))
                    {
                        for (int i = 4; i < lines.Length; i++)
                        {
                            CsvRow row = new CsvRow();
                            row.Add(lines[i]);
                            StringBuilder builder = new StringBuilder();
                            foreach (string value in row)
                            {
                                builder.Append(value);
                            }
                            row.LineText = builder.ToString();
                            writer.WriteLine(row.LineText);
                        }
                        writer.Flush();
                        writer.Close();
                    }
                }
            }

            if (type == "vmmstats" || type == "cpustats")
            {
                if (Count == 0)
                {
                    using (CsvFileWriter writer = new CsvFileWriter(path))
                    {
                        if (type == "vmmstats")
                        {
                            String header = "OS Name,CPU,Time serviced(msec),Halt time(msec),VMM processed time(msec),Total time(msec),Entry count,Exit count,Halt count,Exit Intr count,Min VMM processed time(musec),Max VMM processed time(musec)";
                            writer.WriteLine(header);
                        }
                        else
                        {
                            String header = "CPU,Idle time(msec),Active time(msec),Total time(msec),Idle entry count";
                            writer.WriteLine(header);
                        }
                        for (int i = 1; i < lines.Length; i++)
                        {
                            CsvRow row = new CsvRow();
                            row.Add(lines[i]);
                            StringBuilder builder = new StringBuilder();

                            bool firstColumn = true;
                            foreach (string value in row)
                            {
                                builder.Append(value);
                                firstColumn = false;
                            }
                            row.LineText = builder.ToString();
                            writer.WriteLine(row.LineText);
                        }
                        writer.Flush();
                        writer.Close();
                    }
                }
                else
                {
                    using (StreamWriter writer = File.AppendText(path))
                    {
                        for (int i = 1; i < lines.Length; i++)
                        {
                            CsvRow row = new CsvRow();
                            row.Add(lines[i]);
                            StringBuilder builder = new StringBuilder();
                            foreach (string value in row)
                            {
                                builder.Append(value);
                            }
                            row.LineText = builder.ToString();
                            writer.WriteLine(row.LineText);
                        }
                        writer.Flush();
                        writer.Close();
                    }
                }
            }
        }

        private void StartLogging_Click(object sender, RoutedEventArgs e)
        {
            var logpath = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = logpath.ShowDialog();
            if (result.ToString().Equals("OK"))
            {
                logsavepath = logpath.SelectedPath;
                Log_toCsv.IsEnabled = false;
                StopLog_toCsv.IsEnabled = true;
            }
            else
            {
                Log_toCsv.IsEnabled = true;
                StopLog_toCsv.IsEnabled = false;
            }
        }

        private void StopLogging_Click(object sender, RoutedEventArgs e)
        {
            Log_toCsv.IsEnabled = true;
            StopLog_toCsv.IsEnabled = false;
        }
        private string[] Sort(String[] Lines, String type)
        {
            string[] results = null;
            DataSet dataSet = new DataSet();
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Device", typeof(string));
            dataTable.Columns.Add("State", typeof(string));
            dataTable.Columns.Add("Ontime", typeof(string));
            dataTable.Columns.Add("Offtime", typeof(string));
            dataTable.Columns.Add("Clock", typeof(string));
            for (int i = 0; i < Lines.Length; i++)
            {
                string[] split_name = new string[100];
               // MessageBox.Show(""+Lines.Length);
                split_name = Lines[i].Split(',', ',');
                if (split_name.Count() == 5)
                    dataTable.Rows.Add(split_name[0], split_name[1], split_name[2], split_name[3], split_name[4]);
                //MessageBox.Show("yes");
                if (split_name.Count() == 4)
                    dataTable.Rows.Add(split_name[0], split_name[1], split_name[2], split_name[3], "");
                //MessageBox.Show("" + split_name[0]+""+ split_name[1]+""+ split_name[2]+""+ split_name[3]);
                 // MessageBox.Show("no");
            }
            //MessageBox.Show("2");
            DataView view = dataTable.DefaultView;
            view.Sort = sortwith;
            DataTable sortedDatatable = view.ToTable();
            //sortedDT.ToString();
            results = new string[sortedDatatable.Rows.Count];
            for (int index = 0; index < sortedDatatable.Rows.Count; index++)
            {
                results[index] = sortedDatatable.Rows[index]["Device"].ToString() + "," + sortedDatatable.Rows[index]["State"].ToString() + "," + sortedDatatable.Rows[index]["Ontime"].ToString() + "," + sortedDatatable.Rows[index]["Offtime"].ToString() + "," + sortedDatatable.Rows[index]["Clock"].ToString();
            }
            return results;
        }

        private DispatcherTimer dsa_wakeup_timer;
        private DispatcherTimer dsa_enter_timer;
        private DispatcherTimer dsa_status_timer;

        private void DSA_WakeUp_Timer_Tick(object sender, EventArgs e)
        {
            if (deviceUsb.isConnected("blah"))
            {
                if (deviceUsb.getDsaState() == (int)UsbAdb.DSAFSM.DSLEEP_DONE)
                {
                    /*
                     * We got timer expiry, but we already got a 
                     * notification that system exited from deep sleep
                     */
                    startAnalysis.IsEnabled = true;
                    stopAnalysis.IsEnabled = false;
                    progressbar.Visibility = Visibility.Hidden;
                    Remainingtime.Visibility = Visibility.Hidden;
                }

            }
            else if (deviceUsb.getDsaState() == (int)UsbAdb.DSAFSM.IN_DSLEEP)
            {
                /*
                 * We have not reconnected, but State machine indicates, we entered deep sleep
                 */
                deviceUsb.setDsaState((int)UsbAdb.DSAFSM.DSLEEP_NOT_RECOVERED);
                startAnalysis.IsEnabled = true;
                stopAnalysis.IsEnabled = false;
                progressbar.Visibility = Visibility.Hidden;
                Remainingtime.Visibility = Visibility.Hidden;
            }

            dsa_status_timer.Stop();

            dsa_wakeup_timer.Stop();

            dsa_enter_timer.Stop();
        }

        private void DSA_Enter_Timer_Tick(object sender, EventArgs e)
        {

            if (deviceUsb.getDsaState() == (int)UsbAdb.DSAFSM.WAIT_DSLEEP_ENTER)
            {
                /*
                 * We did not enter deep sleep and timer aborted us
                 */
                deviceUsb.setDsaState((int)UsbAdb.DSAFSM.DSLEEP_ENTER_ABORT);
                startAnalysis.IsEnabled = true;
                stopAnalysis.IsEnabled = false;
                progressbar.Visibility = Visibility.Hidden;
                Remainingtime.Visibility = Visibility.Hidden;
                dsa_wakeup_timer.Stop();
                //dsa_status_timer.Stop();
            }

            dsa_enter_timer.Stop();
            dsa_status_timer.Stop();
            //Remainingtime.Text = "Unknown Error";
        }
        public void DSA_Status_Timer_Tick(object sender, EventArgs e)
        {
            progressbar.Value += 1;

            if (progressbar.Value == progressbar.Maximum)
            {
                Remainingtime.Text = "Completed";
                dsa_status_timer.Stop();
                progressbar.Visibility = Visibility.Hidden;
                Remainingtime.Visibility = Visibility.Hidden;
            }
            else
            {
                long remtime = (long)progressbar.Maximum - (long)progressbar.Value;
                dsa_status_timer.Start();
                Remainingtime.Text = remtime.ToString("N") + " seconds remaining.";
            }

        }

        private int ParseSleepTime(string hours, string minutes)
        {
            int hour = Int16.Parse(hours);
            int minute = Int16.Parse(minutes);
            int time = hour * 3600 + minute * 60;
            return time;
        }

        private void StartDPA_Click(object sender, RoutedEventArgs e)
        {
            int enter_abort_time = 25;
            dsa_grid.Visibility = Visibility.Hidden;
            dsa_summary_title.Visibility = Visibility.Hidden;
            dsa_log_title.Visibility = Visibility.Visible;
            dstates_title.Visibility = Visibility.Hidden;
            dstates_grid.Visibility = Visibility.Hidden;
            ints_grid.Visibility = Visibility.Hidden;
            dsaTextBox.Visibility = Visibility.Visible;

            dsa_grid.Children.Clear();
            dsaTextBox.Document.Blocks.Clear();

            if (TestMode.IsChecked == true)
                test_wo_device = true;
            else
                test_wo_device = false;

            int totaltime = ParseSleepTime(hours.Text, minutes.Text);

            /*
             * Set the total sleep time. This is the expected
             * sleep time that we compare it against with
             */
            dsa.SetSleepTime(totaltime);

            /*
             * Find the use case that user selected
             */
            ComboBoxItem usecase_select = (ComboBoxItem)dsa_usecase.SelectedItem;
            UseCaseProfile ucp = (UseCaseProfile)usecase_select.Tag;
            this.m_SelectedUseCase = ucp;

            /*
             * For testing without device, uncomment the four lines
             */
            if (test_wo_device == true)
            {
                dsa_grid.Visibility = Visibility.Visible;
                dsa_summary_title.Visibility = Visibility.Visible;
                dstates_title.Visibility = Visibility.Visible;
                dstates_grid.Visibility = Visibility.Visible;
                ints_grid.Visibility = Visibility.Visible;

                deviceUsb.setDsaState((int)UsbAdb.DSAFSM.DSLEEP_DONE);
                return;
            }

            Boolean conn = deviceUsb.isConnected("blah");
            if (conn == false)
            {
                if (MessageBox.Show("SoFIA device is not connected.Do you want to continue?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;
            }

            progressbar.Value = progressbar.Minimum = 0;


            /*
             * Give some buffer time
             */
            progressbar.Maximum = totaltime + 30;


            if (ucp.m_StartScript != null)
                CreateProcess_DeviceInfo_WithArgs(ucp.m_StartScript, totaltime.ToString());

            startAnalysis.IsEnabled = false;
            stopAnalysis.IsEnabled = true;

            progressbar.Visibility = Visibility.Visible;
            Remainingtime.Visibility = Visibility.Visible;

            /*
            * Set the USB ADB FSM that we are in DSLEEP enter mode
            */
            deviceUsb.setDsaState((int)UsbAdb.DSAFSM.WAIT_DSLEEP_ENTER);
            dsa_wakeup_timer = new System.Windows.Threading.DispatcherTimer();
            dsa_wakeup_timer.Tick += new EventHandler(DSA_WakeUp_Timer_Tick);
            dsa_wakeup_timer.Interval = new TimeSpan(0, 0, totaltime + 30);
            dsa_wakeup_timer.Start();

            dsa_enter_timer = new System.Windows.Threading.DispatcherTimer();
            dsa_enter_timer.Tick += new EventHandler(DSA_Enter_Timer_Tick);
            dsa_enter_timer.Interval = new TimeSpan(0, 0, enter_abort_time);
            dsa_enter_timer.Start();

            dsa_status_timer = new System.Windows.Threading.DispatcherTimer();
            dsa_status_timer.Tick += new EventHandler(DSA_Status_Timer_Tick);
            dsa_status_timer.Interval = new TimeSpan(0, 0, 1);
            dsa_status_timer.Start();
        }

        private void StopDPA_Click(object sender, RoutedEventArgs e)
        {
            if (deviceUsb.isConnected("blah"))
            {
                if (this.m_SelectedUseCase.m_StopScript != null)
                    CreateProcess_DeviceInfo(this.m_SelectedUseCase.m_StopScript);
            }

            this.m_SelectedUseCase = null;

            startAnalysis.IsEnabled = true;
            stopAnalysis.IsEnabled = false;
            progressbar.Visibility = Visibility.Hidden;
            Remainingtime.Visibility = Visibility.Hidden;
        }
        public List<DSAReasons> Savedreasons = new List<DSAReasons>();
        public List<DeviceStates> SaveddevList = new List<DeviceStates>();
        public List<IRQ> SaveddsaIntList = new List<IRQ>();

        /*
         * @Comment: this is called once the DSA data from the target is retrieved. 
         * Apply the heuristics, process and show the results in UI
         */
        public void ShowDSAResults()
        {
            List<IRQ> dsaIntList = DSA.ParseInterrupts(soc);
            List<PhysCPU> phyCPUList = DSA.ParsePhysicalCPUStats(soc);
            List<VirtCPU> virtCPUList = DSA.ParseVirtualCPUStats(soc);
            List<DeviceStates> devList = DSA.ParseDeepSleepDeviceStates();
            List<DSAReasons> reasons;

            Intresults(dsaIntList);
            phyCpuResults(phyCPUList);
            VirCpuResults(virtCPUList);
            DstateResults(devList);

            reasons = dsa.PerformDeepSleepAnalysis(this.m_SelectedUseCase,
                                    virtCPUList, phyCPUList,
                                    dsaIntList, devList);
            Savedreasons = reasons;
            SaveddevList = devList;
            SaveddsaIntList = dsaIntList;

            DisplayDSAReasons(reasons, dsa_grid);
            DisplayDSADStates(devList, dstates_grid);
            DisplayInts(dsaIntList, ints_grid);

        }

        /*
         * @comment: This function takes a list of Deep Sleep reasons and renders onto 
         * a provided Grid
         */
        public void DisplayDSAReasons(List<DSAReasons> reasons, Grid dispGrid)
        {

            dispGrid.RowDefinitions.Clear();
            dispGrid.ColumnDefinitions.Clear();
            dispGrid.Children.Clear();

            dispGrid.Visibility = Visibility.Visible;


            ColumnDefinition colDef1 = new ColumnDefinition();
            ColumnDefinition colDef2 = new ColumnDefinition();
            ColumnDefinition colDef3 = new ColumnDefinition();
            ColumnDefinition colDef4 = new ColumnDefinition();


            colDef1.Width = new GridLength(65, GridUnitType.Pixel);
            colDef2.Width = new GridLength(100, GridUnitType.Pixel);
            colDef3.Width = new GridLength(330, GridUnitType.Pixel);
            colDef4.Width = new GridLength(400, GridUnitType.Pixel);


            dispGrid.ColumnDefinitions.Add(colDef1);
            dispGrid.ColumnDefinitions.Add(colDef2);
            dispGrid.ColumnDefinitions.Add(colDef3);
            dispGrid.ColumnDefinitions.Add(colDef4);

            //dispGrid.Height = 60 * (reasons.Count()+1);

            for (int j = 0; j <= reasons.Count(); j++)
            {
                RowDefinition rowDef = new RowDefinition();
                //  rowDef.MinHeight = 10;
                //rowDef.Height = new GridLength(1, GridUnitType.Star);
                //rowDef.Height = new GridLength(20, GridUnitType.Pixel);
                dispGrid.RowDefinitions.Add(rowDef);
            }

            for (int z = 0; z < 4; z++)
            {
                TextBlock header = new TextBlock();
                switch (z)
                {
                    case 0:
                        header.Text = "Grading";
                        break;
                    case 1:
                        header.Text = " SubSystem";
                        break;
                    case 2:
                        header.Text = "     Expected Output ";
                        break;
                    case 3:
                        header.Text = "Reason";
                        break;

                }
                header.FontSize = 18;
                if (z == 3)
                {
                    header.TextAlignment = TextAlignment.Center;
                }
                header.Foreground = new SolidColorBrush(Colors.White);
                Grid.SetColumnSpan(header, 3);
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, z);

                SolidColorBrush blueBrush = new SolidColorBrush();
                blueBrush.Color = Colors.Blue;
                Rectangle blueRectangle = new Rectangle();
                blueRectangle.Stroke = new SolidColorBrush(Colors.Gray);
                blueRectangle.Fill = blueBrush;
                Grid.SetRow(blueRectangle, 0);
                Grid.SetColumn(blueRectangle, z);
                dispGrid.Children.Add(blueRectangle);
                dispGrid.Children.Add(header);
                //dispGrid.ShowGridLines = true;

            }


            dispGrid.InvalidateVisual();
            dispGrid.UpdateLayout();

            for (int j = 0; j < reasons.Count(); j++)
            {
                for (int k = 0; k <= 3; k++)
                {
                    TextBlock Tblock = new TextBlock();
                    if (k == 0)
                    {
                        SolidColorBrush bBrush = new SolidColorBrush();

                        string grading = reasons[j].GetGrading();

                        if (String.Compare(grading, "RED", true) == 0)
                        {
                            bBrush.Color = Colors.Red;
                        }

                        if (String.Compare(grading, "YELLOW", true) == 0)
                        {
                            bBrush.Color = Colors.Yellow;
                        }

                        if (String.Compare(grading, "GREEN", true) == 0)
                        {
                            bBrush.Color = Colors.Green;
                        }

                        Rectangle bRectangle = new Rectangle();
                        bRectangle.Fill = bBrush;

                        //dispGrid.Children.Insert(j + 1, bRectangle);
                        Grid.SetColumnSpan(bRectangle, 1);
                        Grid.SetRow(bRectangle, j + 1);
                        Grid.SetColumn(bRectangle, k);
                        dispGrid.Children.Add(bRectangle);
                        //dispGrid.ShowGridLines = true;
                    }
                    else
                    {
                        if (k == 1)
                        {
                            Tblock.Text = reasons[j].GetSubsystem();
                        }
                        else if (k == 2)
                        {
                            Tblock.Text = reasons[j].GetExpectedOutcome();
                            Tblock.TextWrapping = TextWrapping.Wrap;
                        }
                        else
                        {
                            Tblock.Text = reasons[j].GetReason();
                            Tblock.TextWrapping = TextWrapping.Wrap;
                        }
                        Tblock.FontSize = 16;

                        //dispGrid.Children.Insert(j+1, Tblock);
                        Rectangle rectangle = new Rectangle();
                        rectangle.Stroke = new SolidColorBrush(Colors.Gray);
                        Grid.SetColumnSpan(rectangle, 1);
                        Grid.SetRow(rectangle, j + 1);
                        Grid.SetColumn(rectangle, k);
                        Grid.SetColumnSpan(Tblock, 1);
                        Grid.SetRow(Tblock, j + 1);
                        Grid.SetColumn(Tblock, k);
                        dispGrid.Children.Add(Tblock);
                        dispGrid.Children.Add(rectangle);
                        //dispGrid.ShowGridLines = true;
                    }
                }
                dispGrid.InvalidateVisual();
                dispGrid.UpdateLayout();
            }
            dispGrid.UpdateLayout();
            dsa_summary_title.Visibility = Visibility.Visible;
        }

        /*
         * @comment: This function takes a list of Deep Sleep reasons and renders onto 
         * a provided Grid
         */
        public void DisplayDSADStates(List<DeviceStates> devStates, Grid dispGrid)
        {
            int j = 0;
            dispGrid.RowDefinitions.Clear();
            dispGrid.ColumnDefinitions.Clear();
            dispGrid.Children.Clear();

            dispGrid.Visibility = Visibility.Visible;


            ColumnDefinition colDef1 = new ColumnDefinition();
            ColumnDefinition colDef2 = new ColumnDefinition();
            ColumnDefinition colDef3 = new ColumnDefinition();


            colDef1.Width = new GridLength(200, GridUnitType.Pixel);
            colDef2.Width = new GridLength(150, GridUnitType.Pixel);
            colDef3.Width = new GridLength(150, GridUnitType.Pixel);

            dispGrid.ColumnDefinitions.Add(colDef1);
            dispGrid.ColumnDefinitions.Add(colDef2);
            dispGrid.ColumnDefinitions.Add(colDef3);


            /*
             * Add a row for the header
             */
            RowDefinition rowDef = new RowDefinition();
            dispGrid.RowDefinitions.Add(rowDef);

            foreach (DeviceStates dev in devStates)
            {
                if (dev.GetOnTime() > 0)
                {
                    rowDef = new RowDefinition();
                    dispGrid.RowDefinitions.Add(rowDef);
                }
            }

            for (int z = 0; z < 3; z++)
            {
                TextBlock header = new TextBlock();
                switch (z)
                {
                    case 0:
                        header.Text = "Device Name";
                        header.TextAlignment = TextAlignment.Center;
                        break;
                    case 1:
                        header.Text = "Ontime";
                        header.TextAlignment = TextAlignment.Center;
                        break;
                    case 2:
                        header.Text = "Offtime";
                        header.TextAlignment = TextAlignment.Center;
                        break;
                }

                header.FontSize = 18;

                header.Foreground = new SolidColorBrush(Colors.White);
                Grid.SetColumnSpan(header, 1);
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, z);

                SolidColorBrush blueBrush = new SolidColorBrush();
                blueBrush.Color = Colors.Blue;
                Rectangle blueRectangle = new Rectangle();
                blueRectangle.Stroke = new SolidColorBrush(Colors.Gray);
                blueRectangle.Fill = blueBrush;
                Grid.SetRow(blueRectangle, 0);
                Grid.SetColumn(blueRectangle, z);
                dispGrid.Children.Add(blueRectangle);
                dispGrid.Children.Add(header);
                //dispGrid.ShowGridLines = true;

            }


            dispGrid.InvalidateVisual();
            dispGrid.UpdateLayout();

            j = 0;

            foreach (DeviceStates dev in devStates)
            {
                /*
                 * Dont display device that are 100% turned off.
                 */
                if (dev.GetOnTime() == 0)
                    continue;

                for (int k = 0; k < 3; k++)
                {
                    TextBlock Tblock = new TextBlock();
                    if (k == 0)
                    {
                        Tblock.Text = dev.getdevName();
                    }
                    else if (k == 1)
                    {
                        Tblock.Text = dev.GetOnTime().ToString("N") + "ms";
                        Tblock.TextWrapping = TextWrapping.Wrap;
                        Tblock.TextAlignment = TextAlignment.Right;
                    }
                    else
                    {
                        Tblock.Text = dev.GetOffTime().ToString("N") + "ms";
                        Tblock.TextWrapping = TextWrapping.Wrap;
                        Tblock.TextAlignment = TextAlignment.Right;
                    }

                    Tblock.FontSize = 16;

                    //dispGrid.Children.Insert(j+1, Tblock);
                    Rectangle rectangle = new Rectangle();
                    rectangle.Stroke = new SolidColorBrush(Colors.Gray);
                    Grid.SetColumnSpan(rectangle, 1);
                    Grid.SetRow(rectangle, j + 1);
                    Grid.SetColumn(rectangle, k);
                    Grid.SetColumnSpan(Tblock, 1);
                    Grid.SetRow(Tblock, j + 1);
                    Grid.SetColumn(Tblock, k);
                    dispGrid.Children.Add(Tblock);
                    dispGrid.Children.Add(rectangle);
                }

                dispGrid.InvalidateVisual();
                dispGrid.UpdateLayout();
                j++;
            }

            dispGrid.UpdateLayout();
            dstates_title.Visibility = Visibility.Visible;
        }


        public void DisplayInts(List<IRQ> Ints, Grid dispGrid)
        {
            int j = 0;
            dispGrid.RowDefinitions.Clear();
            dispGrid.ColumnDefinitions.Clear();
            dispGrid.Children.Clear();

            dispGrid.Visibility = Visibility.Visible;


            ColumnDefinition colDef1 = new ColumnDefinition();
            ColumnDefinition colDef2 = new ColumnDefinition();
            ColumnDefinition colDef3 = new ColumnDefinition();
            ColumnDefinition colDef4 = new ColumnDefinition();

            colDef1.Width = new GridLength(200, GridUnitType.Pixel);
            colDef2.Width = new GridLength(100, GridUnitType.Pixel);
            colDef3.Width = new GridLength(150, GridUnitType.Pixel);
            colDef4.Width = new GridLength(150, GridUnitType.Pixel);

            dispGrid.ColumnDefinitions.Add(colDef1);
            dispGrid.ColumnDefinitions.Add(colDef2);
            dispGrid.ColumnDefinitions.Add(colDef3);
            dispGrid.ColumnDefinitions.Add(colDef4);


            /*
             * Add a row for the header
             */
            RowDefinition rowDef = new RowDefinition();
            dispGrid.RowDefinitions.Add(rowDef);

            foreach (IRQ dev in Ints)
            {
                if (dev.GetCPUCount() > 0)
                {
                    rowDef = new RowDefinition();
                    dispGrid.RowDefinitions.Add(rowDef);
                }
            }

            for (int z = 0; z < 4; z++)
            {
                TextBlock header = new TextBlock();
                switch (z)
                {
                    case 0:
                        header.Text = "IRQ Name";
                        header.TextAlignment = TextAlignment.Center;
                        break;
                    case 1:
                        header.Text = "IRQ No";
                        header.TextAlignment = TextAlignment.Center;
                        break;
                    case 2:
                        header.Text = "WakeCount(CPU0)";
                        header.TextAlignment = TextAlignment.Center;
                        break;
                    case 3:
                        header.Text = "WakeCount(CPU1)";
                        header.TextAlignment = TextAlignment.Center;
                        break;
                }

                header.FontSize = 18;

                header.Foreground = new SolidColorBrush(Colors.White);
                Grid.SetColumnSpan(header, 1);
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, z);

                SolidColorBrush blueBrush = new SolidColorBrush();
                blueBrush.Color = Colors.Blue;
                Rectangle blueRectangle = new Rectangle();
                blueRectangle.Stroke = new SolidColorBrush(Colors.Gray);
                blueRectangle.Fill = blueBrush;
                Grid.SetRow(blueRectangle, 0);
                Grid.SetColumn(blueRectangle, z);
                dispGrid.Children.Add(blueRectangle);
                dispGrid.Children.Add(header);
                //dispGrid.ShowGridLines = true;

            }


            dispGrid.InvalidateVisual();
            dispGrid.UpdateLayout();

            j = 0;

            foreach (IRQ dev in Ints)
            {


                for (int k = 0; k < 4; k++)
                {
                    TextBlock Tblock = new TextBlock();
                    if (k == 0)
                    {

                        Tblock.Text = dev.GetIrqName();
                        Tblock.TextWrapping = TextWrapping.Wrap;
                        Tblock.TextAlignment = TextAlignment.Left;
                    }
                    else if (k == 1)
                    {
                        Tblock.Text = dev.GetIrqNumber().ToString();
                        Tblock.TextAlignment = TextAlignment.Center;
                    }
                    else if (k == 2)
                    {
                        Tblock.Text = dev.GetWakeInterruptCount(0).ToString();
                        Tblock.TextWrapping = TextWrapping.Wrap;
                        Tblock.TextAlignment = TextAlignment.Center;
                    }
                    else
                    {
                        Tblock.Text = dev.GetWakeInterruptCount(1).ToString();
                        Tblock.TextWrapping = TextWrapping.Wrap;
                        Tblock.TextAlignment = TextAlignment.Center;
                    }
                    Tblock.FontSize = 16;

                    //dispGrid.Children.Insert(j+1, Tblock);
                    Rectangle rectangle = new Rectangle();
                    rectangle.Stroke = new SolidColorBrush(Colors.Gray);
                    Grid.SetColumnSpan(rectangle, 1);
                    Grid.SetRow(rectangle, j + 1);
                    Grid.SetColumn(rectangle, k);
                    Grid.SetColumnSpan(Tblock, 1);
                    Grid.SetRow(Tblock, j + 1);
                    Grid.SetColumn(Tblock, k);
                    dispGrid.Children.Add(Tblock);
                    dispGrid.Children.Add(rectangle);
                }

                dispGrid.InvalidateVisual();
                dispGrid.UpdateLayout();
                j++;
            }
            dispGrid.UpdateLayout();
            ints_title.Visibility = Visibility.Visible;
        }


        public void DstateResults(List<DeviceStates> dDeltaDeviceStates)
        {
            dsaTextBox.AppendText("Device States Statistics in Deep Sleep:\n");

            foreach (DeviceStates dev in dDeltaDeviceStates)
            {
                if (dev == null)
                    continue;

                if (dev.GetOnTime() > 0)
                    dsaTextBox.AppendText("\t Name:" + dev.getdevName() +
                                            " OnTime: " + dev.GetOnTime() +
                                            " offtime:" + dev.GetOffTime() + " \n");
            }

        }

        public void Intresults(List<IRQ> dsaIntList)
        {
            dsaTextBox.AppendText("Interrupts:\n");

            foreach (IRQ irq in dsaIntList)
            {
                if (irq.GetTotalWakeInterruptCounts() == 0)
                    continue;

                dsaTextBox.AppendText("\t Name:" + irq.GetIrqName() + " WakeCount: " + irq.GetTotalWakeInterruptCounts() + "\n");
            }
        }


        public void phyCpuResults(List<PhysCPU> phyCPUList)
        {
            phyCPUList = DSA.ParsePhysicalCPUStats(soc);

            foreach (PhysCPU pcpu in phyCPUList)
            {
                dsaTextBox.AppendText("Physical CPU:" + pcpu.GetCoreInstance() + "\n");

                float idleres = ((float)pcpu.GetIdleTime() / (float)pcpu.GetTotalTime()) * 100;
                float avgidleres = (float)pcpu.GetIdleTime() / (float)pcpu.GetIdleCount();

                dsaTextBox.AppendText("\t Idle residency:" + idleres + "%\n");
                dsaTextBox.AppendText("\t Average Idle Residency:" + avgidleres + "ms\n");
                dsaTextBox.AppendText("\t Idle time (ms):" + pcpu.GetIdleTime() + " ms\n");
                dsaTextBox.AppendText("\t Total time (ms):" + pcpu.GetTotalTime() + " ms\n");

            }

        }

        public void VirCpuResults(List<VirtCPU> virtCPUList)
        {
            virtCPUList = DSA.ParseVirtualCPUStats(soc);

            foreach (VirtCPU vcpu in virtCPUList)
            {
                dsaTextBox.AppendText("VM:" + vcpu.GetVMName() + "\n");
                dsaTextBox.AppendText("Virtual CPU" + vcpu.GetCoreInstance() + "\n");

                float idleres = ((float)vcpu.GetIdleTime() / (float)vcpu.GetTotalTime()) * 100;
                float avgidleres = (float)vcpu.GetIdleTime() / (float)vcpu.GetIdleCount();

                dsaTextBox.AppendText("\t Idle residency:" + idleres + "%\n");
                dsaTextBox.AppendText("\t Average Idle Residency:" + avgidleres + "ms\n");
                dsaTextBox.AppendText("\t Idle time (ms):" + vcpu.GetIdleTime() + " ms\n");
                dsaTextBox.AppendText("\t Total time (ms):" + vcpu.GetTotalTime() + " ms\n");
            }

        }

        private void DurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void dpa_Click(object sender, RoutedEventArgs e)
        {
            Delete_Items();
            dpa.IsEnabled = true;
            tabControl1.Items.Insert(0, dpa);
            tabControl1.SelectedItem = dpa;
            isQuickLaunched = true;

            /*
             * Populate the use cases list
             */
            dsa_usecase.Items.Clear();
            foreach (UseCaseProfile ucp in this.m_UseCaseList)
            {
                ComboBoxItem useCase = new ComboBoxItem();
                useCase.Content = ucp.m_DisplayName;
                useCase.Tag = ucp;
                dsa_usecase.Items.Add(useCase);
            }

            dsa_usecase.SelectedIndex = 0;
        }

        public class DeviceProfile
        {
            public string m_DevName;
            public float m_Residency = 0;
            public float m_OptVar = 0;
            public float m_NonOptVar = 0;

            public DeviceProfile(string name)
            {
                this.m_DevName = name;
            }

        }

        public class InterruptProfile
        {
            public string m_IntName;
            public float m_Interval = 0;
            public float m_OptVar = 0;
            public float m_NonOptVar = 0;

            public InterruptProfile(string name)
            {
                this.m_IntName = name;
            }

        }

        public class CPUProfile
        {
            public int m_CpuType;
            public string m_CPUName;
            public int m_CPUInstance;
            public float m_OptIdleResidency = 0;
            public float m_OptAverageIdleResidency = 0;
            public float m_NonOptIdleResidency = 0;
            public float m_NonOptAverageIdleResidency = 0;

            public CPUProfile(string type, string name, int instance)
            {
                if (String.Compare(type, "Virtual", true) == 0)
                    this.m_CpuType = 0;

                if (String.Compare(type, "Physical", true) == 0)
                    this.m_CpuType = 1;

                this.m_CPUName = name;
                this.m_CPUInstance = instance;
            }
        }
        public class UseCaseProfile
        {
            public string m_DisplayName;
            public string m_StartScript;
            public string m_StopScript;
            public bool m_ParamFile;
            public List<DeviceProfile> m_DevProfileList;
            public List<InterruptProfile> m_IntProfileList;
            public List<CPUProfile> m_CPUProfile;

            /*
             * @comment Parse Use Case from XML node into a class
             */
            public UseCaseProfile(System.Xml.Linq.XElement useCase)
            {
                this.m_DisplayName = useCase.Element("Name").Value.Trim();
                var cpuNodes = useCase.Element("CPUS").Elements("CPU");
                var intNodes = useCase.Element("Interrupts").Elements("Interrupt");
                var devNodes = useCase.Element("Devices").Elements("Device");

                this.m_DevProfileList = new List<DeviceProfile>();
                this.m_IntProfileList = new List<InterruptProfile>();
                this.m_CPUProfile = new List<CPUProfile>();

                try
                {
                    /*
                     * Parse the Script nodes
                     */
                    var scriptNodes = useCase.Element("Script").Elements("Local");
                    foreach (System.Xml.Linq.XElement scr in scriptNodes)
                    {
                        var startnode = scr.Attribute("Start");
                        if (startnode != null)
                            this.m_StartScript = startnode.Value.Trim();

                        var stopnode = scr.Attribute("Stop");
                        if (stopnode != null)
                            this.m_StopScript = stopnode.Value.Trim();
                    }
                }
                catch (Exception) { }

                /*
                 * Parse the CPU nodes
                 */
                foreach (System.Xml.Linq.XElement cpu in cpuNodes)
                {
                    CPUProfile cpuProf = ParseUseCaseCPU(cpu);
                    this.m_CPUProfile.Add(cpuProf);
                }

                /*
                 * Parse the Interrupts
                 */
                foreach (System.Xml.Linq.XElement intr in intNodes)
                {
                    InterruptProfile intrProf = ParseUseCaseInterrupts(intr);
                    this.m_IntProfileList.Add(intrProf);
                }

                /*
                 * Parse the device residency information
                 */
                foreach (System.Xml.Linq.XElement dev in devNodes)
                {
                    DeviceProfile devProf = ParseUseCaseDevices(dev);
                    this.m_DevProfileList.Add(devProf);
                }

            }
            public static UseCaseProfile ParseUseCase(System.Xml.Linq.XElement useCase)
            {
                return null;
            }

            private static CPUProfile ParseUseCaseCPU(System.Xml.Linq.XElement cpu)
            {
                CPUProfile cpuProf;
                string type = cpu.Attribute("type").Value.Trim();

                string name = cpu.Attribute("name").Value.Trim();
                int instance = Int32.Parse(cpu.Attribute("Instance").Value.Trim());
                cpuProf = new CPUProfile(type, name, instance);

                System.Xml.Linq.XElement IR = cpu.Element("IdleResidency");
                System.Xml.Linq.XElement AIR = cpu.Element("AverageResidency");
                System.Xml.Linq.XElement OptIR = IR.Element("Optimal");
                System.Xml.Linq.XElement NonOptIR = IR.Element("NonOptimal");
                System.Xml.Linq.XElement OptAIR = AIR.Element("Optimal");
                System.Xml.Linq.XElement NonOptAIR = AIR.Element("NonOptimal");

                cpuProf.m_OptIdleResidency = (float)Double.Parse(OptIR.Attribute("Value").Value.Trim());
                cpuProf.m_NonOptIdleResidency = (float)Double.Parse(NonOptIR.Attribute("Value").Value.Trim());
                cpuProf.m_OptAverageIdleResidency = (float)Double.Parse(OptAIR.Attribute("Value").Value.Trim());
                cpuProf.m_NonOptAverageIdleResidency = (float)Double.Parse(NonOptAIR.Attribute("Value").Value.Trim());

                return cpuProf;
            }
            private static InterruptProfile ParseUseCaseInterrupts(System.Xml.Linq.XElement intr)
            {
                InterruptProfile intProf;
                string name = intr.Attribute("Name").Value.Trim();
                float interval = (float)Double.Parse(intr.Attribute("Interval").Value.Trim());
                float OptVar = (float)Double.Parse(intr.Attribute("Optimal").Value.Trim());
                float NonOptVar = (float)Double.Parse(intr.Attribute("NonOptimal").Value.Trim());

                intProf = new InterruptProfile(name);
                intProf.m_Interval = interval;
                intProf.m_OptVar = OptVar;
                intProf.m_NonOptVar = NonOptVar;
                return intProf;
            }
            private static DeviceProfile ParseUseCaseDevices(System.Xml.Linq.XElement dev)
            {
                DeviceProfile devProf;
                string name = dev.Attribute("Name").Value.Trim();
                float residency = (float)Double.Parse(dev.Attribute("Residency").Value.Trim());
                float OptVar = (float)Double.Parse(dev.Attribute("Optimal").Value.Trim());
                float NonOptVar = (float)Double.Parse(dev.Attribute("NonOptimal").Value.Trim());

                devProf = new DeviceProfile(name);
                devProf.m_Residency = residency;
                devProf.m_OptVar = OptVar;
                devProf.m_NonOptVar = NonOptVar;
                return devProf;
            }
            public static List<UseCaseProfile> LoadUseCaseProfiles(SoFIA soc)
            {
                List<UseCaseProfile> useCasesList;
                UseCaseProfile profile;

                useCasesList = new List<UseCaseProfile>();

                System.Xml.Linq.XDocument xmlfile = System.Xml.Linq.XDocument.Load("UseCaseProfile_sf3g.xml");

                var nodes = xmlfile.Element("ProfileList").Elements("Profile");

                foreach (System.Xml.Linq.XElement item in nodes)
                {
                    profile = new UseCaseProfile(item);
                    useCasesList.Add(profile);
                }

                return useCasesList;
            }

            public CPUProfile FindPhysicalCoreProfile(int instance)
            {
                foreach (CPUProfile cpu in m_CPUProfile)
                {
                    /*
                     * Skip virtual CPU's
                     */
                    if (cpu.m_CpuType == 0)
                        continue;

                    if (cpu.m_CPUInstance == instance)
                        return cpu;
                }
                return null;
            }
        }
        private void Sortwith(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            var point = Mouse.GetPosition(p_res_grid);
            int col = 0;
            double accumulatedWidth = 0.0;

            // calc col mouse was over
            foreach (var columnDefinition in p_res_grid.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                col++;
            }
            if (col == 2)
            {
                sortwith = "Ontime ASC";
                DisplayAfterClick("dstates", sortwith);
            }
            if (col == 3)
            {
                sortwith = "Offtime ASC";
                DisplayAfterClick("dstates", sortwith);

            }
        }

        private void Stop_Interupts(object sender, RoutedEventArgs e)
        {

            if (IntsStatsTimer.IsEnabled == true)
                IntsStatsTimer.Stop();
            intstop.IsEnabled = false;
            int_refreshButton.IsEnabled = false;
            intsRefreshCombo.IsEnabled = false;
            intstart.IsEnabled = true;
            if (!Log_toCsv.IsEnabled)
            {
                WriteToCsv("ints", DstatesCount, "ints");
                Interruptcount++;
            }

        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Delete_Items();
            Setting.IsEnabled = true;
            tabControl1.Items.Insert(0, Setting);
            tabControl1.SelectedItem = Setting;
            isQuickLaunched = true;
        }

        private void IntStartButton_Click(object sender, RoutedEventArgs e)
        {
            //String name = AppDomain.CurrentDomain.BaseDirectory + "\\capabilities.xml";
            //string i= null;
            Boolean conn = deviceUsb.isConnected("blah");
            if (conn == true)
            {             
                SetBackground();
                CreateProcess_DeviceInfo("get_interrupts.bat");
                //i = GetCount();
                Display_Ints("irqs");

                IntsStatsTimer = new System.Windows.Threading.DispatcherTimer();

                if (!intref_dis.IsSelected)
                {
                    int refresh_time = 2;

                    if (intref_3sec.IsSelected)
                        refresh_time = 3;

                    if (intref_5sec.IsSelected)
                        refresh_time = 5;

                    IntsStatsTimer.Tick += new EventHandler(intsTimer_Tick);
                    IntsStatsTimer.Interval = new TimeSpan(0, 0, refresh_time);
                    IntsStatsTimer.Start();
                }
                intstop.IsEnabled = true;
                int_refreshButton.IsEnabled = true;
                intsRefreshCombo.IsEnabled = true;
                intstart.IsEnabled = false;
            }
            else
            {
                if (MessageBox.Show("SoFIA device is not connected.Do you want to continue?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {

                    SetBackground();
                    CreateProcess_DeviceInfo("get_interrupts.bat");
                    Display_Ints("irqs");

                    IntsStatsTimer = new System.Windows.Threading.DispatcherTimer();

                    if (!intref_dis.IsSelected)
                    {
                        int refresh_time = 2;

                        if (intref_3sec.IsSelected)
                            refresh_time = 3;

                        if (intref_5sec.IsSelected)
                            refresh_time = 5;

                        IntsStatsTimer.Tick += new EventHandler(intsTimer_Tick);
                        IntsStatsTimer.Interval = new TimeSpan(0, 0, refresh_time);
                        IntsStatsTimer.Start();
                    }
                    intstop.IsEnabled = true;
                    int_refreshButton.IsEnabled = true;
                    intsRefreshCombo.IsEnabled = true;
                    intstart.IsEnabled = false;
                }
            }
        }

        public string[] parseDevstates()
        {
            XmlDocument doc = new XmlDocument();
            string xmlUrl = AppDomain.CurrentDomain.BaseDirectory + "dstates/residency.xml";
            doc.Load(xmlUrl);
            XmlNodeList elemList = doc.GetElementsByTagName("device");
            string[] line = new string[elemList.Count];
            for (int i = 0; i < elemList.Count; i++)
            {
                string name = elemList[i].Attributes["name"].Value;
                string mode = elemList[i].Attributes["mode"].Value;
                string ontime = elemList[i].Attributes["on_time"].Value;
                string offtime = elemList[i].Attributes["off_time"].Value;
                string clock = elemList[i].Attributes["clock"].Value;
                line[i] = name + "," + mode + "," + ontime + "," + offtime+","+clock;
               // MessageBox.Show(""+line[i]);
            }
            return line;
        }

        public string[] parseInterrupts(int cpu)
        {
            XmlDocument doc = new XmlDocument();
            // MessageBox.Show("1");

            string xmlUrl = AppDomain.CurrentDomain.BaseDirectory + "dstates/irqs.xml";
            //MessageBox.Show("2");
            doc.Load(xmlUrl);
            string start_time=null, read_time = null;
            //XmlNodeList elemList = doc.GetElementsByTagName("irq");
            XmlNodeList elemList1 = doc.GetElementsByTagName("irqs");
            for (int i = 0; i < elemList1.Count; i++)
            {
                start_time = elemList1[i].Attributes["start_time"].Value;
                read_time = elemList1[i].Attributes["read_time"].Value;
            }

           
            XmlNodeList elemList = doc.GetElementsByTagName("irq");
            string[] line = new string[elemList.Count];
            for (int i = 0; i < elemList.Count; i++)
            {
                string id = elemList[i].Attributes["id"].Value;
                string cpu0_count = elemList[i].Attributes["cpu0_count"].Value;
                string cpu0_wakecount = elemList[i].Attributes["cpu0_wakecount"].Value;
                string cpu1_count = elemList[i].Attributes["cpu1_count"].Value;
                string cpu1_wakecount = elemList[i].Attributes["cpu1_wakecount"].Value;
                
                if (cpu > 2)
                {
                  //  MessageBox.Show("1");
                    string cpu2_count = elemList[i].Attributes["cpu2_count"].Value;

                    string cpu2_wakecount = elemList[i].Attributes["cpu2_wakecount"].Value;
                    string cpu3_count = elemList[i].Attributes["cpu3_count"].Value;
                    string cpu3_wakecount = elemList[i].Attributes["cpu3_wakecount"].Value;

                    line[i] = id + "," + start_time + "," + read_time + "," + cpu0_count + "," + cpu0_wakecount + "," + cpu1_count + "," + cpu1_wakecount + "," + cpu2_count + "," + cpu2_wakecount + "," + cpu3_count + "," + cpu3_wakecount;
                }
                else
                {
                    line[i] = id + "," + start_time + "," + read_time + "," + cpu0_count + "," + cpu0_wakecount + "," + cpu1_count + "," + cpu1_wakecount;
                }
            }
            return line;
        }

        public string[] parseVirtualstats()
        {
            XmlDocument doc = new XmlDocument();
            string xmlUrl = AppDomain.CurrentDomain.BaseDirectory + "dstates/virtual.xml";
            doc.Load(xmlUrl);
            XmlNodeList elemList = doc.GetElementsByTagName("device");
            string[] line = new string[elemList.Count];
           // MessageBox.Show(""+elemList.Count);
            for (int i = 0; i < elemList.Count; i++)
            {
                string vm = elemList[i].Attributes["vm"].Value;
                string name = elemList[i].Attributes["name"].Value;
                string service_time = elemList[i].Attributes["service_time"].Value;
                string entry_count = elemList[i].Attributes["entry_count"].Value;
                string exit_count = elemList[i].Attributes["exit_count"].Value;
                string total_time = elemList[i].Attributes["total_time"].Value;
                string halt_time = elemList[i].Attributes["halt_time"].Value;
                string halt_count = elemList[i].Attributes["halt_count"].Value;
                string proc_time = elemList[i].Attributes["proc_time"].Value;
                string min_time = elemList[i].Attributes["min_time"].Value;
                string max_time = elemList[i].Attributes["max_time"].Value;
                string intr_count = elemList[i].Attributes["intr_count"].Value;
                line[i] = vm +","+ name + "," + service_time + "," + entry_count + "," + exit_count + "," +total_time+ "," +halt_time+ "," +halt_count+
                    "," +proc_time+ "," +min_time+ "," +max_time+ "," +intr_count;
            }
            return line;
        }
        public string[] parsePhysicalstats()
        {
            XmlDocument doc = new XmlDocument();
            string xmlUrl = AppDomain.CurrentDomain.BaseDirectory + "dstates/physical.xml";
            doc.Load(xmlUrl);
            XmlNodeList elemList = doc.GetElementsByTagName("cpu");
            string[] line = new string[elemList.Count];
            for (int i = 0; i < elemList.Count; i++)
            {
                string id = elemList[i].Attributes["id"].Value;
                string idle_time = elemList[i].Attributes["idle_time"].Value;
                string active_time = elemList[i].Attributes["active_time"].Value;
                string total_time = elemList[i].Attributes["total_time"].Value;
                string entry_time = elemList[i].Attributes["entry_time"].Value;

                line[i] = id + "," + idle_time + "," + active_time + "," + total_time + "," + entry_time;
                //MessageBox.Show("lines are"+line[i]);
            }
            return line;
        }

        public static string GetCount()
        {
            String cpucount = null;
            string xmlUrl = AppDomain.CurrentDomain.BaseDirectory + "capabilities.xml";
            XmlDocument xmlDoc = new XmlDocument();           
            //xmlDoc.Load(xmlUrl);
            //XmlNode CpuListNode =
            //   xmlDoc.SelectSingleNode("/capabilities");
            XmlNodeList CpuNodeList = xmlDoc.GetElementsByTagName("capabilities");
            for (int i = 0; i < CpuNodeList.Count; i++)
            {
                cpucount = CpuNodeList[i].Attributes["cpus"].Value;
            }
           // MessageBox.Show("cpu count is:"+cpucount);
            return cpucount;
        }

        public void SaveDSA_Click(object sender, RoutedEventArgs e)
        {
            var Savepath = new System.Windows.Forms.FolderBrowserDialog();
            //string path = Savepath.ToString();
            System.Windows.Forms.DialogResult result = Savepath.ShowDialog();
           // MessageBox.Show("" + Savepath.SelectedPath);
            savereasons(Savepath.SelectedPath);
            savedstates(Savepath.SelectedPath);
        }

        public void savereasons(string path)
        {            
            string Savepath = path + "\\DsaSummary.txt";
            //string grading = null, SubSystem = null, ExpectedOutcome = null, reason = null;
            using (CsvFileWriter writer = new CsvFileWriter(Savepath))
            {
                string header = "Grading,SubSystem,ExpectedOutcome,Reason";
                writer.WriteLine(header);
                StringBuilder builder = new StringBuilder();
                //MessageBox.Show("" + Savedreasons[1].GetGrading());
                for (int j = 1; j < Savedreasons.Count(); j++)
                {
                    string[] line = new string[100];
                    line[j] = Savedreasons[j].GetGrading() + "," + Savedreasons[j].GetSubsystem() + "," + Savedreasons[j].GetExpectedOutcome() + "," + Savedreasons[j].GetReason();
                    //System.IO.StreamWriter file = new System.IO.StreamWriter(Savepath,true);
                    //file.WriteLine(line[j]);
                    //file.Close();
                    CsvRow row = new CsvRow();
                    row.Add(line[j]);
                    foreach (string value in row)
                    {
                        builder.Append(value);
                    }
                    row.LineText = builder.ToString();
                    writer.WriteLine(row.LineText);
                }
                writer.Flush();
                writer.Close();
            }
        }

        public void savedstates(String path)
        {
            string Savepath = path + "\\DsaSummary.csv";
            using (CsvFileWriter writer = new CsvFileWriter(Savepath))
            {
                string header = "DeviceName,OnTime(ms),OffTime(ms)";
                writer.WriteLine(header);
                StringBuilder builder = new StringBuilder();
                foreach (DeviceStates dev in SaveddevList)
                {                  
                    if (dev.GetOnTime() == 0)
                        continue;
                    string line = null;
                    line = dev.getdevName() + "," + dev.GetOnTime().ToString("N") + ","+ dev.GetOffTime().ToString("N");

                    CsvRow row = new CsvRow();
                    row.Add(line);

                    foreach (string value in row)
                    {
                        builder.Append(value);
                    }
                    row.LineText = builder.ToString();
                    writer.WriteLine(row.LineText);
                }
                writer.Flush();
                writer.Close();
                }
            }

        private void LoadDsa_Click(object sender, RoutedEventArgs e)
        {
            //parseInterrupts();
            //parseVirtualstats();
        }

             
    }
}





