using Bank_Bot.Models;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Bank_Bot
{
    public class AzureManager
    {

        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<BankAccountInformation> timelineTable;

        private AzureManager()
        {
            this.client = new MobileServiceClient("http://maxbankbot.azurewebsites.net");
            this.timelineTable = this.client.GetTable<BankAccountInformation>();
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        public async Task<List<BankAccountInformation>> GetTimelines()
        {
            return await this.timelineTable.ToListAsync();
        }

        public async Task AddTimeline(BankAccountInformation BAInfo)
        {
            await this.timelineTable.InsertAsync(BAInfo);
        }

        public async Task UpdateTimeline(BankAccountInformation BAInfo)
        {
            await this.timelineTable.UpdateAsync(BAInfo);
        }

        public async Task<BankAccountInformation> getAccountFromName(string Name)
        {
            List<BankAccountInformation> accounts = await GetTimelines();
            foreach (BankAccountInformation baI in accounts)
            {
                if(baI.Name.ToLower() == Name.ToLower())
                {
                    return baI;
                }
            }

            return null;
        }

        public async Task<BankAccountInformation> getAccountFromNumber(string Number)
        {
            List<BankAccountInformation> accounts = await GetTimelines();
            foreach (BankAccountInformation baI in accounts)
            {
                if (baI.accountNumber == Number)
                {
                    return baI;
                }
            }

            return null;
        }

        public async Task<BankAccountInformation> payAccount(string name, string amount)
        {
            List<BankAccountInformation> accounts = await GetTimelines();
            foreach (BankAccountInformation baI in accounts)
            {
                if(baI.Name.ToLower().Trim(' ') == name.ToLower().Trim(' ') || baI.accountNumber == name)
                {
                    baI.Money += Convert.ToInt32(amount);
                    return baI;
                }
            }
            return null;
        }

        public async Task<BankAccountInformation> removeFromAccount(string name, string amount)
        {
            List<BankAccountInformation> accounts = await GetTimelines();
            foreach (BankAccountInformation baI in accounts)
            {
                if (baI.Name.ToLower().Trim(' ') == name.ToLower().Trim(' ') || baI.accountNumber == name)
                {
                    baI.Money -= Convert.ToInt32(amount);
                    return baI;
                }
            }
            return null;
        }

    }
}