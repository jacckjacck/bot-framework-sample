using BotSample.Model;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BotSample.Dialogs
{
    [Serializable]
    public class BookDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }


        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;


            string _bookingExample;
            BookLUIS _bookLUIS = await GetEntityFromLUIS(message.Text);

            if (_bookLUIS.intents.Count() > 0)
            {
                switch (_bookLUIS.intents[0].intent)
                {
                    case "Saludo":
                        await context.PostAsync("Hola!, ¿cuál es tu nombre?");
                        break;
                    case "nombres":
                        var _entities = _bookLUIS.entities.Where(x => x.type == "detalle_nombre");
                        if (_entities.Count() > 0)
                        {
                            var _userName = _entities.First().entity;
                            _userName = char.ToUpper(_userName[0]) + _userName.Substring(1);
                            //Save the user name 
                            context.UserData.SetValue("UserName", _userName);
                            await context.PostAsync($"Hola {_userName}, ¿Desde y hacia dónde quieres viajar?");
                        }
                        break;
                    case "AgendaViaje":
                        var entityFrom = _bookLUIS.entities.Where(x => x.type == "Location::FromLocation");
                        var entityTo = _bookLUIS.entities.Where(x => x.type == "Location::ToLocation");
                        string _message = string.Empty;

                        if (entityFrom.Count() > 0)
                        {
                            context.UserData.SetValue("bookFrom", entityFrom.First().entity);
                        }
                        else
                        {
                            PromptDialog.Text(context, null
                                , "¿Desde y hacia dónde quieres viajar?"
                                ,"No logramos entender"
                                );
                            return;
                        }

                        if (entityTo.Count() > 0)
                        {
                            context.UserData.SetValue("bookTo", entityTo.First().entity);
                        }
                        else
                        {
                            PromptDialog.Text(context, null
                               , "¿Desde y hacia dónde quieres viajar?"
                               , "No logramos entender"
                               );
                            return;
                        }


                        PromptDialog.Text(context
                               , AfterEmailAskAsync
                               , $"Perfecto { context.UserData.GetValue<string>("UserName")}, enviaremos un mail con la información de las fechas disponibles, regálanos un email"
                               , ""
                               );

                        break;
                    default:
                        await context.PostAsync("Lo sentimos, no entendemos la petición, ¿Es posible que seas un poco más descriptivo?, gracias!");
                        break;
                }
            }
        }

        private async Task AfterEmailAskAsync(IDialogContext context, IAwaitable<string> result)
        {
            string _to = await result;
            var apiKey = "<<your sendgrid api key>>";
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("jorgea.gomez@hotmail.com", "Jorge Gómez");
            var subject = $"Reservación Bot desde {context.UserData.GetValue<string>("bookFrom")} hacia {context.UserData.GetValue<string>("bookTo")} ";
            var to = new EmailAddress($"{_to}", $"{context.UserData.GetValue<string>("UserName")}");
            var htmlContent = $"<strong>Hola {context.UserData.GetValue<string>("UserName")}</strong>, <br/>"+
                "Esta son las fechas disponibles para tu viaje: <br/>"+
                "2019/01/01 - 2018/12/31 <br/>"+
                "Saludos!";

            var msg = MailHelper.CreateSingleEmail(from, to, subject,"",htmlContent);
            var response = await client.SendEmailAsync(msg);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                await context.PostAsync("Revisa la bandeja de entrada, hemos enviado un correo con la información!");
            }
        }

        private static async Task<BookLUIS> GetEntityFromLUIS(string query)
        {
            query = Uri.EscapeDataString(query);
            BookLUIS _data = new BookLUIS();

            using (HttpClient client = new HttpClient())
            {
                string requestURI = $"<<your LUIS api key>>";

                HttpResponseMessage msg = await client.GetAsync(requestURI);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    _data = JsonConvert.DeserializeObject<BookLUIS>(JsonDataResponse);
                }
            }
            return _data;
        }
        
    }
}