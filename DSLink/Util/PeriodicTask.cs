using System;
using System.Threading;
using System.Threading.Tasks;

namespace DSLink.Util
{
    public class PeriodicTask
    {
        private readonly Action _func;
        private readonly int _millisecondDelay;
        private readonly CancellationTokenSource _tokenSource;
        private Task _periodicTask;
        
        public PeriodicTask(Action func, int millisecondDelay)
        {
            _func = func;
            _millisecondDelay = millisecondDelay;
            _tokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            if (_periodicTask != null && !_periodicTask.IsCanceled)
            {
                Stop();
            }
            _periodicTask = Task.Factory.StartNew(_taskLoop, _tokenSource.Token);
        }

        public void Stop()
        {
            if (_periodicTask != null && !_tokenSource.IsCancellationRequested)
            {
                _tokenSource.Cancel();
            }
        }

        private void _taskLoop()
        {
            while (_periodicTask.Status != TaskStatus.Canceled)
            {
                _func();
                Thread.Sleep(_millisecondDelay);
            }
        }
    }
}
