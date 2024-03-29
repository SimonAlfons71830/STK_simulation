using OSPABA;
using simulation;
using agents;
using DISS_SEM2;
using DISS_SEM2.Generators;

namespace managers
{
	//meta! id="2"
	public class ManagerOkolia : Manager
	{
		private int id; //id counter for customer

        public ManagerOkolia(int id, Simulation mySim, Agent myAgent) :
			base(id, mySim, myAgent)
		{
			Init();
			this.id = 0;
		}

		override public void PrepareReplication()
		{
			base.PrepareReplication();
			// Setup component for the next replication
			this.id = 0;
			if (PetriNet != null)
			{
				PetriNet.Clear();
			}
            
        }

		//meta! sender="AgentModelu", id="17", type="Notice"
		public void ProcessCustomerDeparture(MessageForm message)
		{
			this.MyAgent.localAverageCustomerCountInSTK.addValues(this.MyAgent.CustomersCount, MySim.CurrentTime - this.MyAgent.localAverageCustomerCountInSTK.timeOfLastChange);
			this.MyAgent.localAverageCustomerCountInSTK.timeOfLastChange = MySim.CurrentTime;

			var pompom = this.MyAgent.CustomersCount;
			this.MyAgent.CustomersCount--;

			var pom = MySim.CurrentTime - ((MyMessage)message).customer.arrivalTime;
			var arrivalTime = ((MyMessage)message).customer.arrivalTime;
            this.MyAgent.localAverageCustomerTimeInSTK.addValues(MySim.CurrentTime - ((MyMessage)message).customer.arrivalTime);

			

            this.MyAgent.customersThatLeft.Enqueue(((MyMessage)message), ((MyMessage)message).DeliveryTime);

            //ZAKAZNIK ODISIEL
        }

		//meta! sender="PlanerCustomerArrival", id="39", type="Finish"
		public void ProcessFinish(MessageForm message)
		{
            //vygenerovany zakaznik sa posle do agenta modelu
            var newMessage = new MyMessage(MySim)
            {
                Addressee = MySim.FindAgent(SimId.AgentModelu),
				Code = Mc.CustomerArrival,
                //generacia noveho zakaznika s casom ktory prisiel po ukonceni assistenta
                customer = new Customer(MySim.CurrentTime, new Car(this.MyAgent.carTypeGenerator.Next()))
			};
            newMessage.customer._id = this.id;
			this.id++;
            Notice(newMessage);

            this.MyAgent.localAverageCustomerCountInSTK.addValues(this.MyAgent.CustomersCount, MySim.CurrentTime - this.MyAgent.localAverageCustomerCountInSTK.timeOfLastChange);
            this.MyAgent.localAverageCustomerCountInSTK.timeOfLastChange = MySim.CurrentTime;

            this.MyAgent.CustomersCount++;

			//chladenie
			if (MySim.CurrentTime < 24300) // 6*60+45  *  60 
			{
                message.Addressee = MyAgent.FindAssistant(SimId.PlanerCustomerArrival);
                StartContinualAssistant(message);
            }
            
        }

		//meta! sender="AgentModelu", id="15", type="Notice"
		public void ProcessInicialization(MessageForm message)
		{
            //posle sa prvy zakaznik
            var newMessage = new MyMessage(MySim)
            {
                Addressee = MySim.FindAgent(SimId.AgentModelu),
                Code = Mc.CustomerArrival,
                //generacia noveho zakaznika s casom ktory prisiel po ukonceni assistenta
                customer = new Customer(0, new Car(this.MyAgent.carTypeGenerator.Next()))
				
            };
			((MyMessage)newMessage).customer._id = this.id;
			this.id++;
            Notice(newMessage);

            this.MyAgent.localAverageCustomerCountInSTK.addValues(this.MyAgent.CustomersCount, MySim.CurrentTime - this.MyAgent.localAverageCustomerCountInSTK.timeOfLastChange);
            this.MyAgent.localAverageCustomerCountInSTK.timeOfLastChange = MySim.CurrentTime;

            this.MyAgent.CustomersCount++;

            message.Addressee = MyAgent.FindAssistant(SimId.PlanerCustomerArrival);
            StartContinualAssistant(message);
        }

		//meta! userInfo="Process messages defined in code", id="0"
		public void ProcessDefault(MessageForm message)
		{
			switch (message.Code)
			{
			}
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

			case Mc.CustomerDeparture:
				ProcessCustomerDeparture(message);
			break;

			case Mc.Finish:
				ProcessFinish(message);
			break;

			default:
				ProcessDefault(message);
			break;
			}
		}
		//meta! tag="end"
		public new AgentOkolia MyAgent
		{
			get
			{
				return (AgentOkolia)base.MyAgent;
			}
		}
	}
}