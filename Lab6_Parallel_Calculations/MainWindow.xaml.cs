using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Threading;

namespace Lab6_Parallel_Calculations
{
    public class Data
    {
        public int n { set; get; }
        public int step { set; get; }
        public int start { set; get; }
        public long Summ { set; get; }
        public ManualResetEvent Event { set; get; }
        public Data(int n, int step, int start, ManualResetEvent Event)
        {
            this.n = n;
            this.Summ = 0;
            this.start = start;
            this.step = step;
            this.Event = Event;
        }
        public Data(int n)
        {
            this.n = n;
            this.Summ = 0;
            this.start = 0;
            this.step = 1;
            this.Event = null;
        }
        public void Set()
        {
            Event.Set();
        }
    }
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool[] SimpleFlags = new bool[long.Parse("100000000")];
        public long[] Simpls = null;
        public void markSimples(long step){
            for (long i = 2 * step; i < SimpleFlags.LongLength; i += step)
                SimpleFlags[i] = true;
        }
        static void WaitAll(WaitHandle[] waitHandles)
        {
            if (Thread.CurrentThread.ApartmentState == ApartmentState.STA)
                // WaitAll для STA не поддерживается, поэтому делаем это вручную
                foreach (WaitHandle myWaitHandle in waitHandles)
                    WaitHandle.WaitAny(new WaitHandle[] { myWaitHandle });
            else
                //Вызываем стандартный метод
                WaitHandle.WaitAll(waitHandles);
        } 
        public void findSimpleNumbers(long n)
        {
            long[] Simpls = new long[n];
            long j = 2, i = 0;
            while (i < n)
            {
                if (!SimpleFlags[j])
                {
                    markSimples(j);
                    Simpls[i] = j;
                    i++;
                }
                j++;
            }
            this.Simpls = Simpls;
        }
        public long findTreadSumm(int n, int threadsNumber)
        {
            Data[] data = new Data[threadsNumber];
            Thread[] threads = new Thread[threadsNumber];
            ManualResetEvent[] handles = new ManualResetEvent[threadsNumber];
            int i = 0;
            while (i < n && i < threadsNumber)
            {
                handles[i] = new ManualResetEvent(false);
                data[i] = new Data(n, threadsNumber, i, handles[i]);
                threads[i] = new Thread(findAsyncSumm);
                threads[i].Start(data[i]);
                i++;
            }
            long Summ = 0;
            i = 0;
            WaitAll(handles);
            while (i < n && i < threadsNumber)
            {
                Summ += data[i].Summ;
                i++;
            }
            return Summ;
        }
        public void findAsyncSumm(object data)
        {
            Data vars = (Data)data;
            int n = vars.n, step = vars.step;
            long Summ = 0;
            for (int i = vars.start; i < n; i += step)
                Summ += Simpls[i];
            ((Data)data).Summ = Summ;
            ((Data)data).Set();
        }
        public long findSyncSumm(int n)
        {
            long Summ = 0;
            for (int i = 0; i < n; i++)
                Summ += Simpls[i];
            return Summ;
        }
        public MainWindow()
        {
            InitializeComponent();
            findSimpleNumbers(long.Parse("1000000"));
        }

        private void ASync_Click(object sender, RoutedEventArgs e)
        {
            DoExpirements(true);
        }
        private void Sync_Click(object sender, RoutedEventArgs e)
        {
            DoExpirements();
        }
        public void DoExpirements(bool async = false)
        {
            Stopwatch globalWatch = new Stopwatch(), sWatch = new Stopwatch();
            globalWatch.Start();
            int threadsNumb = int.Parse(inputText.Text);
            int[] expirements = { 10, 100, 500, 1000, 2000, 5000, 10000, 50000, 100000, 500000, 1000000 };
            resultText.Text = "Эксперимент\tКол-во чисел\tРезультат\tВремя\n";            
            long Summ = async ? findTreadSumm(expirements[0], threadsNumb) : findSyncSumm(expirements[0]);
            for (int i = 0; i < expirements.Length; i++)
            {
                sWatch.Start();
                Summ = async ? findTreadSumm(expirements[i], threadsNumb) : findSyncSumm(expirements[i]);
                sWatch.Stop();
                resultText.Text += (i + 1) + "\t\t" + expirements[i] + "\t\t" + Summ + (Summ < 9999999 ? "\t":"") + "\t" + sWatch.ElapsedTicks + ".\n";
                sWatch.Reset();
            }
            globalWatch.Stop();
            resultText.Text += "Затраченное время: " + globalWatch.ElapsedMilliseconds + " мс.";
        }
    }
}
