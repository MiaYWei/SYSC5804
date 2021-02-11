/*
*This program is the solution for SYSC5804 Assignment 2:The task manager for at least five typical embedded tasks to replace or bypass 
* The task manager can manage multiple tasks by priority, task priority 0 as highest
* When any “event” occurs , and the corresponding task is trigged, the task manager determines which task to run next. 
* The tasks states can be running, ready, suspend and finished.
* 
* Author: Mia Wei 
* Date: 2021-02-05
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TaskManager
{
    class Program
    {
        static void Main(string[] args)
        {
            TaskItem task_1 = new TaskItem("1st Task", 1, 1);
            TaskItem task_2 = new TaskItem("2nd Task", 2, 5);
            TaskItem task_3 = new TaskItem("3rd Task", 3, 3);
            TaskItem task_4 = new TaskItem("4th Task", 4, 7);
            TaskItem task_5 = new TaskItem("5th Task", 5, 2);

            TaskManager tc = new TaskManager();

            new Thread(() => { tc.Process(); }).Start();
            new Thread(() => { Thread.Sleep(2500); tc.Add(task_5); }).Start();
            new Thread(() => { Thread.Sleep(5000); tc.Add(task_4); }).Start();
            new Thread(() => { Thread.Sleep(7500); tc.Add(task_3); }).Start();
            new Thread(() => { Thread.Sleep(10000); tc.Add(task_2); }).Start();
            new Thread(() => { Thread.Sleep(12500); tc.Add(task_1); }).Start();
        }
    }

    public class TaskItem
    {
        public TaskItem(string name, int priority, int consumption)
        {
            Name = name;
            Priority = priority;
            ResourceConsumption = consumption;
            Console.WriteLine($"\"{Name}\" initialized: priority: {Priority} ; required resource: {consumption} ");
        }

        public string Name { get; set; } = "Task";
        public int Priority { get; set; } = 5;
        public string Status { get; set; } = "Ready";
        public int ResourceConsumption { get; set; } = 5; //Default value is 5
        public double Progress { get; set; } = 0.001;
        private readonly System.Timers.Timer timer = new System.Timers.Timer() { Interval = 100 }; // Timer used for task process check. 0.1 second

        public void Start()
        {
            timer.Elapsed += (sender, e) =>
            {
                Progress += 0.01;
                if (Progress >= 1)  //Assume all the tasks process time are same, which is 0.1* 100 = 10 seconds
                {
                    timer.Stop();
                    Status = "Finished";
                }
            };
            timer.Start();
            Status = "Running";
        }

        public void Suspend()
        {
            timer.Stop();
            Status = "Suspend";
        }
    }

    public class TaskManager
    {
        public bool Running { get; set; } = false;
        public int Capacity { get; set; } = 10;  //Total reousrce capacity
        private readonly List<TaskItem> TaskQueue = new List<TaskItem>();

        public void Add(TaskItem item)
        {
            lock (TaskQueue) 
            {
                TaskQueue.Add(item);
                Console.WriteLine($"Adds \"{item.Name}\" to task container.");
            }
        }

        public void Process()
        {
            Running = true;
            while (Running)
            {
                lock (TaskQueue) 
                {
                    foreach(var finishedItem in TaskQueue.Where(i => i.Status == "Finished").ToList())
                    {
                        TaskQueue.Remove(finishedItem);
                        Console.WriteLine($"\"{finishedItem.Name}\" is finished and removed form task container.");
                    }
                }

                lock (TaskQueue)
                {
                    var topPriorityItem = TaskQueue.Where(i => i.Status == "Ready" || i.Status == "Suspend").OrderBy(i => i.Priority).FirstOrDefault();

                    if (topPriorityItem == null) continue;

                    if (Capacity >= TaskQueue.Where(i => i.Status == "Running").Select(i => i.ResourceConsumption).Sum() + topPriorityItem.ResourceConsumption)
                    {
                        Console.WriteLine($"\"{topPriorityItem.Name}\" starts to Running from {topPriorityItem.Status} state.");
                        topPriorityItem.Start();
                        continue;
                    }

                    var itemNeedToSuspend = TaskQueue.Where(i => i.Status == "Running").OrderByDescending(i => i.Priority).FirstOrDefault();
                    if (null != itemNeedToSuspend && null != topPriorityItem && topPriorityItem.Priority < itemNeedToSuspend.Priority)
                    {
                        itemNeedToSuspend.Suspend();
                        Console.WriteLine($"\"{itemNeedToSuspend.Name}\" is suspended.");
                    }
                }
            }
        }
    }
}
