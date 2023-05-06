using OSPABA;
using simulation;
using agents;
using continualAssistants;
using DISS_SEM2;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace managers
{
	//meta! id="3"
	public class ManagerSTK : Manager
	{
		public ManagerSTK(int id, Simulation mySim, Agent myAgent) :
			base(id, mySim, myAgent)
		{
			Init();
		}

		override public void PrepareReplication()
		{
			base.PrepareReplication();
			// Setup component for the next replication

			if (PetriNet != null)
			{
				PetriNet.Clear();
			}
		}

		//meta! sender="AgentModelu", id="18", type="Request"
		public void ProcessCustomerService(MessageForm message)
		{
            //pride novy zakaznik
            //posle sa na obsadenie parkovacieho miesta
            this.MyAgent.totalCustomers++;

            //STAT
            this.MyAgent.localAverageCustomerCountToTakeOver.addValues(this.MyAgent.takeoverqueue.Count, MySim.CurrentTime - this.MyAgent.localAverageCustomerCountToTakeOver.timeOfLastChange);
            this.MyAgent.localAverageCustomerCountToTakeOver.timeOfLastChange = MySim.CurrentTime;
            this.MyAgent.takeoverqueue.Enqueue(((MyMessage)message), ((MyMessage)message).DeliveryTime);
            //END STAT

            message.Addressee = MySim.FindAgent(SimId.AgentService);
            message.Code = Mc.AssignParkingSpace;
            Request(message);
		}

		//meta! sender="AgentService", id="53", type="Response"
		public void ProcessPayment(MessageForm message)
		{
            //payment zakaznika dokonana
            //uvolnenie technika 
            //pridelenie mu roboty

            this.MyAgent.localAverageFreeTechnicianCount.addValues(this.MyAgent.getAvailableTechniciansCount(),
                       MySim.CurrentTime - this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange);
            this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange = MySim.CurrentTime;

            ((MyMessage)message).technician.obsluhuje = false;
            ((MyMessage)message).technician.state = 0;
			((MyMessage)message).technician.customer_car = null;
            ((MyMessage)message).technician = null;


            //zakaznik po plateni odchadza
            message.Code = Mc.CustomerService;
			message.Addressee = MySim.FindAgent(SimId.AgentModelu);
			Response(message);

            //nova praca
            var technic = this.getAvailableTechnician();
            if (technic != null)
            {
                if (this.MyAgent.paymentLine.Count > 0)
                {
                    this.MyAgent.localAverageFreeTechnicianCount.addValues(this.MyAgent.getAvailableTechniciansCount(),
                       MySim.CurrentTime - this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange);
                    this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange = MySim.CurrentTime;

                    var paymentMessage = this.MyAgent.paymentLine.Dequeue();

                    paymentMessage.technician = technic;
                    paymentMessage.technician.obsluhuje = true;
                    paymentMessage.technician.customer_car = paymentMessage.customer;
                    paymentMessage.technician.state = 2; //platenie

                    paymentMessage.Code = Mc.Payment;
                    paymentMessage.Addressee = MySim.FindAgent(SimId.AgentService);

                    Request(paymentMessage);
                }
                else if (this.MyAgent.waitingForTakeOverAssigned.Count > 0)
                {
                    
                    var instantTakeOver = this.MyAgent.waitingForTakeOverAssigned.Dequeue();

                    //STATS
                    this.MyAgent.localAverageTimeToTakeOverCar.addValues(MySim.CurrentTime - ((MyMessage)instantTakeOver).customer.arrivalTime);
                    this.MyAgent.localAverageCustomerCountToTakeOver.addValues(this.MyAgent.takeoverqueue.Count, MySim.CurrentTime - this.MyAgent.localAverageCustomerCountToTakeOver.timeOfLastChange);
                    this.MyAgent.localAverageCustomerCountToTakeOver.timeOfLastChange = MySim.CurrentTime;
                    this.MyAgent.takeoverqueue.Dequeue();
                    //STATS END

                    this.MyAgent.localAverageFreeTechnicianCount.addValues(this.MyAgent.getAvailableTechniciansCount(),
                        MySim.CurrentTime - this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange);
                    this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange = MySim.CurrentTime;

                    instantTakeOver.technician = technic;
                    instantTakeOver.technician.obsluhuje = true;
                    instantTakeOver.technician.customer_car = instantTakeOver.customer;
                    instantTakeOver.technician.state = 1; //kontrola

                    

                    instantTakeOver.Code = Mc.CarTakeover;
                    instantTakeOver.Addressee = MySim.FindAgent(SimId.AgentService);
                    Request(instantTakeOver);

                }
                else 
                {
                    //nikto necaka v radoch
                    return;
                }
            }
            else
            {
                //nemalo by nastat lebo sa predtym uvolnil
                return;
            }

        }

		//meta! sender="AgentInspection", id="52", type="Response"
		public void ProcessInspection(MessageForm message)
		{
            //mechanik skoncil - uvolni sa + berie sa dalsie auto z radu cakania na inspection


            this.MyAgent.localAverageFreeAutomechanicCount.addValues(this.MyAgent.getAvailableAutomechanicsCount(),
                        MySim.CurrentTime - this.MyAgent.localAverageFreeAutomechanicCount.timeOfLastChange);
            this.MyAgent.localAverageFreeAutomechanicCount.timeOfLastChange = MySim.CurrentTime;

            ((MyMessage)message).automechanic.obsluhuje = false;
			((MyMessage)message).automechanic.customer_car = null;
			((MyMessage)message).automechanic = null;

			//automechanik pokracuje v praci vypyta si z garaze
            //az ked mu je auto priradene tak sa posle message na uvolnenie z garaze
			if (this.MyAgent.waitingForInspection.Count > 0 )
			{
				var automechanic = this.getAvailableAutomechanic();
				if (automechanic != null)
				{
					var inspectionMessage = this.MyAgent.waitingForInspection.Dequeue();

                    this.MyAgent.localAverageFreeAutomechanicCount.addValues(this.MyAgent.getAvailableAutomechanicsCount(),
                        MySim.CurrentTime - this.MyAgent.localAverageFreeAutomechanicCount.timeOfLastChange);
                    this.MyAgent.localAverageFreeAutomechanicCount.timeOfLastChange = MySim.CurrentTime;

                    ((MyMessage)inspectionMessage).automechanic = automechanic;
                    ((MyMessage)inspectionMessage).automechanic.obsluhuje = true;
                    ((MyMessage)inspectionMessage).automechanic.customer_car = ((MyMessage)inspectionMessage).customer;

                    inspectionMessage.Code = Mc.Inspection;
					inspectionMessage.Addressee = MySim.FindAgent(SimId.AgentInspection);
					Request(inspectionMessage);

                    //kopirovat spravu lebo posielam do dvoch agentov
					var copiedMessage = inspectionMessage.CreateCopy();
                    //var copiedMessage = new MyMessage(((MyMessage)inspectionMessage));
                    copiedMessage.Code = Mc.FreeParkingSpace;
                    copiedMessage.Addressee = MySim.FindAgent(SimId.AgentService);
                    Request(copiedMessage);


                }
                else
                {

                    //stale by mal byt nejaky volny lebo sa predtym uvolni 
                    return;
                }
			}

            //vyhlada volneho technika a zakaznik ktory prisiel z inspection moze ist platit
			//ak nieje volny technik ide do radu na platenie
            var technic = this.getAvailableTechnician();
			if (technic != null)
			{
                this.MyAgent.localAverageFreeTechnicianCount.addValues(this.MyAgent.getAvailableTechniciansCount(),
                       MySim.CurrentTime - this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange);
                this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange = MySim.CurrentTime;

                ((MyMessage)message).technician = technic;
				((MyMessage)message).technician.customer_car = ((MyMessage)message).customer;
                ((MyMessage)message).technician.obsluhuje = true;
                ((MyMessage)message).technician.state = 2; //plati

				message.Code = Mc.Payment;
				message.Addressee = MySim.FindAgent(SimId.AgentService);
				Request(message);
			}
			else
			{
				MyAgent.paymentLine.Enqueue(((MyMessage)message), ((MyMessage)message).DeliveryTime);
			}

		}

		//meta! sender="SchedulerLunchBreak", id="65", type="Finish"
		public void ProcessFinishSchedulerLunchBreak(MessageForm message)
		{
		}

		//meta! sender="AgentService", id="33", type="Response"
		public void ProcessCarTakeover(MessageForm message)
		{
            //auto je v garazi
            //uvolni sa technik
            //priradi sa automechanik (front cakajucich na inspection)
            //technikovi sa prideli nova robota

            this.MyAgent.localAverageFreeTechnicianCount.addValues(this.MyAgent.getAvailableTechniciansCount(),
                       MySim.CurrentTime - this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange);
            this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange = MySim.CurrentTime;

            //uvolnenie technika
            ((MyMessage)message).technician.obsluhuje = false;
            ((MyMessage)message).technician.state = 0; //nic nerobi
			((MyMessage)message).technician.customer_car = null;
			((MyMessage)message).technician = null;

            //zistit ci je volny automechanik ak ano, tak mozem zacat inspekciu + poslat notice o uvolneni parkovacieho miesta
            var mechanic = this.getAvailableAutomechanic();
			if (mechanic != null)
			{
                this.MyAgent.localAverageFreeAutomechanicCount.addValues(this.MyAgent.getAvailableAutomechanicsCount(),
                        MySim.CurrentTime - this.MyAgent.localAverageFreeAutomechanicCount.timeOfLastChange);
                this.MyAgent.localAverageFreeAutomechanicCount.timeOfLastChange = MySim.CurrentTime;

                ((MyMessage)message).automechanic = mechanic;
                ((MyMessage)message).automechanic.obsluhuje = true;
                ((MyMessage)message).automechanic.customer_car = ((MyMessage)message).customer;

                message.Code = Mc.Inspection;
				message.Addressee = MySim.FindAgent(SimId.AgentInspection);
				Request(message);

				//kopirovat spravu lebo posielam do dvoch agentov
				var copiedMessage = message.CreateCopy();
				//var copiedMessage = new MyMessage(((MyMessage)message));
				copiedMessage.Code = Mc.FreeParkingSpace;
				copiedMessage.Addressee = MySim.FindAgent(SimId.AgentService);
				Request(copiedMessage);
			}
			else
			{
				this.MyAgent.waitingForInspection.Enqueue(((MyMessage)message), ((MyMessage)message).DeliveryTime);
            }




            //pridelenie roboty technikovi
            var technic = this.getAvailableTechnician();
            if (technic != null)
            {
                if (this.MyAgent.paymentLine.Count > 0)
                {
                    this.MyAgent.localAverageFreeTechnicianCount.addValues(this.MyAgent.getAvailableTechniciansCount(),
                       MySim.CurrentTime - this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange);
                    this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange = MySim.CurrentTime;

                    var paymentMessage = this.MyAgent.paymentLine.Dequeue();

                    paymentMessage.technician = technic;
                    paymentMessage.technician.obsluhuje = true;
                    paymentMessage.technician.customer_car = paymentMessage.customer;
                    paymentMessage.technician.state = 2; //platenie

                    paymentMessage.Code = Mc.Payment;
                    paymentMessage.Addressee = MySim.FindAgent(SimId.AgentService);

                    Request(paymentMessage);
				}
				else if (this.MyAgent.waitingForTakeOverAssigned.Count>0)
				{
                  
					//moze ist hned na takeover uz ma miesto
					var instantTakeOver = this.MyAgent.waitingForTakeOverAssigned.Dequeue();


                    this.MyAgent.localAverageFreeTechnicianCount.addValues(this.MyAgent.getAvailableTechniciansCount(),
                        MySim.CurrentTime - this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange);
                    this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange = MySim.CurrentTime;

                    instantTakeOver.technician = technic;
                    instantTakeOver.technician.obsluhuje = true;
                    instantTakeOver.technician.customer_car = instantTakeOver.customer;
                    instantTakeOver.technician.state = 1; //kontrola

                    //STATS
                    this.MyAgent.localAverageTimeToTakeOverCar.addValues(MySim.CurrentTime - ((MyMessage)instantTakeOver).customer.arrivalTime);
                    this.MyAgent.localAverageCustomerCountToTakeOver.addValues(this.MyAgent.takeoverqueue.Count, MySim.CurrentTime - this.MyAgent.localAverageCustomerCountToTakeOver.timeOfLastChange);
                    this.MyAgent.localAverageCustomerCountToTakeOver.timeOfLastChange = MySim.CurrentTime;
                    this.MyAgent.takeoverqueue.Dequeue();
                    //END STATS

                    instantTakeOver.Code = Mc.CarTakeover;
					instantTakeOver.Addressee = MySim.FindAgent(SimId.AgentService);
					Request(instantTakeOver);

				}
            }
        }

		//meta! sender="AgentService", id="19", type="Response"
		public void ProcessAssignParkingSpace(MessageForm message)
		{
			//uz je pridelene parkovacie miesto
			//priradim technika, ak nieje dam do frontu na cakanie na prevzatie uz assigned
			var technic = this.getAvailableTechnician();
			if (technic != null)
			{

                this.MyAgent.localAverageFreeTechnicianCount.addValues(this.MyAgent.getAvailableTechniciansCount(),
                        MySim.CurrentTime - this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange);
                this.MyAgent.localAverageFreeTechnicianCount.timeOfLastChange = MySim.CurrentTime;

                ((MyMessage)message).technician = technic;
                technic.obsluhuje = true;
                technic.customer_car = ((MyMessage)message).customer;
                technic.state = 1; //kontroluje

                //STAT
                this.MyAgent.localAverageTimeToTakeOverCar.addValues(MySim.CurrentTime - ((MyMessage)message).customer.arrivalTime);
                this.MyAgent.localAverageCustomerCountToTakeOver.addValues(this.MyAgent.takeoverqueue.Count, MySim.CurrentTime - this.MyAgent.localAverageCustomerCountToTakeOver.timeOfLastChange);
                this.MyAgent.localAverageCustomerCountToTakeOver.timeOfLastChange = MySim.CurrentTime;
                this.MyAgent.takeoverqueue.Dequeue();
                //STATS END

                message.Code = Mc.CarTakeover;
                message.Addressee = MySim.FindAgent(SimId.AgentService);
                Request(message);
            }
			else
			{
				this.MyAgent.waitingForTakeOverAssigned.Enqueue(((MyMessage)message), message.DeliveryTime);
			}

		}

		//meta! sender="AgentModelu", id="55", type="Notice"
		public void ProcessInicialization(MessageForm message)
		{
            //o 2 hodiny nastavit obednu prestavku

		}

		//meta! userInfo="Process messages defined in code", id="0"
		public void ProcessDefault(MessageForm message)
		{
			switch (message.Code)
			{
			}
		}

		//meta! sender="Lunch", id="82", type="Finish"
		public void ProcessFinishLunch(MessageForm message)
		{
		}

		//meta! userInfo="Generated code: do not modify", tag="begin"
		public void Init()
		{
		}

		override public void ProcessMessage(MessageForm message)
		{
			switch (message.Code)
			{
			case Mc.Inicialization:
				ProcessInicialization(message);
			break;

			case Mc.Finish:
				switch (message.Sender.Id)
				{
				case SimId.Lunch:
					ProcessFinishLunch(message);
				break;

				case SimId.SchedulerLunchBreak:
					ProcessFinishSchedulerLunchBreak(message);
				break;
				}
			break;

			case Mc.Payment:
				ProcessPayment(message);
			break;

			case Mc.Inspection:
				ProcessInspection(message);
			break;

			case Mc.CarTakeover:
				ProcessCarTakeover(message);
			break;

			case Mc.AssignParkingSpace:
				ProcessAssignParkingSpace(message);
			break;

			case Mc.CustomerService:
				ProcessCustomerService(message);
			break;

			default:
				ProcessDefault(message);
			break;
			}
		}
		//meta! tag="end"
		public new AgentSTK MyAgent
		{
			get
			{
				return (AgentSTK)base.MyAgent;
			}
		}

        public Technician getAvailableTechnician()
        {
            for (int i = 0; i < MyAgent.technicians.Count; i++)
            {
                if (!MyAgent.technicians[i].obsluhuje)
                {
                    return MyAgent.technicians[i];
                }
            }
            return null;
        }
        public Automechanic getAvailableAutomechanic()
        {
            for (int i = 0; i < MyAgent.automechanics.Count; i++)
            {
                if (!MyAgent.automechanics[i].obsluhuje)
                {
                    return MyAgent.automechanics[i];
                }
            }
            return null;
        }
		
    }
}