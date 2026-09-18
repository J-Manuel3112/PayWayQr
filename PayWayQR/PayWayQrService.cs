using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PayWayQR
{
    public class PaywayQrService
    {
        private const string AuthUrl = "https://api-sandbox.payway.com.ar/v1/oauth/accesstoken";
        private const string PaymentsUrl = "https://api-sandbox.payway.com.ar/v1/paystore_terminals/terminal_payments/payments";

        // Datos para probar en Sandbox
        private const string SandboxCuit = "30-12345678-9";
        private const string SandboxTerminalId = "12345678";
        private const string SandboxMerchantGroupCode = "1238494234";

        private readonly string _apiKeyBase64;
        private readonly string _apiKeyPublica;

        public PaywayQrService(string apiKeyBase64, string apiKeyPublica)
        {
            _apiKeyBase64 = apiKeyBase64;
            _apiKeyPublica = apiKeyPublica;
        }
        private async Task<string> ObtenerTokenAsync(HttpClient client)
        {
            var authBody = new { grant_type = "client_credentials" };
            var content = new StringContent(
                JsonConvert.SerializeObject(authBody),
                Encoding.UTF8,
                "application/json"
            );

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + _apiKeyBase64);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await client.PostAsync(AuthUrl, content);
            var responseData = await response.Content.ReadAsStringAsync();

            //Si no da un status 2xx, lanzo una excepción con el mensaje de error
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error OAuth ({response.StatusCode}): {responseData}");

            
            var token = JObject.Parse(responseData)["access_token"]?.ToString();

            //busco el token, si no lo encuentra lanza una excepción
            if (string.IsNullOrEmpty(token))
                throw new Exception("No se obtuvo access_token. Respuesta: " + responseData);

            return token;
        }

        public async Task<string> EnviarCobroAsync(decimal monto)
        {

            using (var client = new HttpClient())
            {
                var accessToken = await ObtenerTokenAsync(client);

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken.Trim());
                client.DefaultRequestHeaders.Add("apikey", _apiKeyPublica.Trim());
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                //convierto ya que así lo espera Payway, en centimos y como string
                string montoCentavos = (monto * 100).ToString("0");

                //Estructura de la petición requerida en payway
                var paymentBody = new
                {
                    payment_request_data = new
                    {
                        subnet_acquirer_id = "1",
                        payment_amount = montoCentavos,
                        terminal_menu_text = $"Pedido de ${monto:0.00}".Replace('.', ','), // Formato correcto
                        ecr_provider = "Software Company",
                        ecr_name = "Software Name",
                        ecr_version = "1.0",
                        change_amount = "0",
                        ecr_transaction_id = (string)null,
                        installments_number = 1,
                        bank_account_type = (string)null,
                        payment_plan_id = (string)null,
                        payment_type = (string)null,
                        print_method = "MOBITEF_NON_FISCAL",
                        print_copies = "BOTH",
                        print_preview = "NONE",
                        terminals_list = new[] { new { terminal_id = SandboxTerminalId } },
                        card_brand_product = (string)null,
                        terminal_operation_method = "CARD", // para usar en sandbox "CARD", ya que así lo pide payway, para uso real usar "QR_CODE"
                        qr_benefit_code = true,  
                        trx_receipt_notes = (string)null,
                        card_holder_id = (string)null,
                        merchant_group_code = SandboxMerchantGroupCode,
                        is_tip = true,
                        currency_code = "032"
                    }
                };

                var jsonContent = new StringContent(
                    JsonConvert.SerializeObject(paymentBody),
                    Encoding.UTF8,
                    "application/json"
                );

                string url = $"{PaymentsUrl}?cuit_cuil={SandboxCuit}";

                var response = await client.PostAsync(url, jsonContent);
                var responseData = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Error al crear pago ({response.StatusCode}): {responseData}");

                var json = JObject.Parse(responseData);
                return json["payment_data"]?["payment_id"]?.ToString()
                       ?? json["payment_id"]?.ToString()
                       ?? responseData;
            }
        }
    }
}