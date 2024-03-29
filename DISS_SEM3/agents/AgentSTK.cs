using OSPABA;
using simulation;
using managers;
using continualAssistants;
using DISS_SEM2;
using Priority_Queue;
using System.Collections.Generic;
using DISS_SEM3.statistics;
using OSPDataStruct;
using System.Text;

namespace agents
{
	//meta! id="3"
	public class AgentSTK : Agent
	{
        public SimplePriorityQueue<MyMessage, double> customersLine;
		public SimplePriorityQueue<MyMessage, double> paymentLine;

        public SimQueue<MyMessage> takeoverqueueQ;
        public SimplePriorityQueue<MyMessage,double> takeoverqueue;

        public List<Technician> technicians;
        public List<Automechanic> automechanics;
        public SimplePriorityQueue<MyMessage, double> waitingForInspection;
        public SimplePriorityQueue<MyMessage, double> waitingForTakeOverAssigned;

        public Statistics localAverageTimeToTakeOverCar { get; set; }
        public WStatistics localAverageCustomerCountToTakeOver { get; set; }
        public WStatistics localAverageFreeTechnicianCount { get; set; }
        public WStatistics localAverageFreeAutomechanicCount { get; set; }

        public int totalCustomers;
        public SimQueue<Technician> technicians_queue;

        public AgentSTK(int id, Simulation mySim, Agent parent) :
			base(id, mySim, parent)
		{
			Init();
			this.customersLine = new SimplePriorityQueue<MyMessage, double>();
			this.paymentLine = new SimplePriorityQueue<MyMessage, double>();
            this.technicians = new List<Technician>();
			this.automechanics = new List<Automechanic>();
            this.waitingForInspection = new SimplePriorityQueue<MyMessage, double>();
            this.waitingForTakeOverAssigned = new SimplePriorityQueue<MyMessage, double>();

            this.localAverageTimeToTakeOverCar = new Statistics();
            this.localAverageCustomerCountToTakeOver =  new WStatistics();
            this.localAverageFreeTechnicianCount = new WStatistics();
            this.localAverageFreeAutomechanicCount = new WStatistics();
            this.takeoverqueueQ = new SimQueue<MyMessage>(new OSPStat.WStat(MySim));
            this.takeoverqueue = new SimplePriorityQueue<MyMessage, double>();

            this.totalCustomers = 0;
            //this.technicians_queue = new SimQueue<Technician>(new OSPStat.WStat(MySim));
        }

		override public void PrepareReplication()
		{
			base.PrepareReplication();
			// Setup component for the next replication
            this.customersLine.Clear();
            this.paymentLine.Clear();
            this.takeoverqueue.Clear();
            this.takeoverqueueQ.Clear();
            
            for (int i = 0; i < this.technicians.Count; i++)
            {
                this.technicians[i].obsluhuje = false;
                this.technicians[i].obeduje = false;
                this.technicians[i].obedoval = false;
                this.technicians[i].customer_car = null;
                this.technicians[i].state = 0;
            }
            for (int i = 0; i < this.automechanics.Count; i++)
            {
                this.automechanics[i].obsluhuje = false;
                this.automechanics[i].obedoval = false;
                this.automechanics[i].obeduje = false;
                this.automechanics[i].customer_car = null;
            }
            this.waitingForInspection.Clear();
            this.waitingForTakeOverAssigned.Clear();

            this.localAverageTimeToTakeOverCar.resetStatistic();
            this.localAverageCustomerCountToTakeOver.resetStatistic();
            this.localAverageFreeTechnicianCount.resetStatistic();
            this.localAverageFreeAutomechanicCount.resetStatistic();

            this.totalCustomers = 0;

        }

		//meta! userInfo="Generated code: do not modify", tag="begin"
		private void Init()
		{
			new ManagerSTK(SimId.ManagerSTK, MySim, this);
			new Lunch(SimId.Lunch, MySim, this);
			new SchedulerLunchBreak(SimId.SchedulerLunchBreak, MySim, this);
			AddOwnMessage(Mc.Payment);
			AddOwnMessage(Mc.CustomerService);
			AddOwnMessage(Mc.Inspection);
			AddOwnMessage(Mc.CarTakeover);
			AddOwnMessage(Mc.AssignParkingSpace);
			AddOwnMessage(Mc.Inicialization);
		}
		//meta! tag="end"

        public void createTechnicians(int number)
        {
            for (int i = 0; i < number; i++)
            {
                var technic = new Technician();
                technic._id = i+1;
                this.technicians.Add(technic);
            }
        }

        public void createAutomechanics(int number, int certification)
        {
            if (((MySimulation)MySim).validationMode)
            {
                for (int i = 0; i < number; i++)
                {
                    var mechanic = new Automechanic();
                    mechanic._id = i + 1;
                    this.automechanics.Add(mechanic);
                }
            }
            else
            {
                var pom = 0;
                for (int i = 0; i < number; i++)
                {
                    var mechanic = new Automechanic();
                    mechanic._id = i + 1;
                    if (pom < certification)
                    {
                        mechanic.certificate = true;
                        pom++;
                    }
                    this.automechanics.Add(mechanic);
                }
            }
            
        }
        public int getAvailableTechniciansCount()
        {
            var pom = 0;
            for (int i = 0; i < this.technicians.Count; i++)
            {
                if (!this.technicians[i].obsluhuje)
                {
                    pom++;
                }
            }
            return pom;
        }
        public int getAvailableAutomechanicsCount()
        {
            var pom = 0;
            for (int i = 0; i < this.automechanics.Count; i++)
            {
                if (!this.automechanics[i].obsluhuje)
                {
                    pom++;
                }
            }
            return pom;
        }

        public void resetAutomechanics()
        {
            for (int i = 0; i < this.automechanics.Count; i++)
            {
                this.automechanics[i].obsluhuje = false;
                this.automechanics[i].obeduje = false;
                this.automechanics[i].obedoval = false;
                this.automechanics[i].customer_car = null;
            }
        }

        public void resetTechnicians()
        {
            for (int i = 0; i < this.technicians.Count; i++)
            {
                this.technicians[i].obsluhuje = false;
                this.technicians[i].obeduje = false;
                this.technicians[i].obedoval = false;
                this.technicians[i].customer_car = null;

            }
        }

    }
}