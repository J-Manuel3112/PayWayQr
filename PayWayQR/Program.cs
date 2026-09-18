using System;
using System.Net;
using System.Threading.Tasks;


namespace PayWayQR
{
    class Program
    {
        static async Task Main(string[] args)
        {

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


            // esto se busca en la misma plataforma de Payway
            string apiKeyBase64 = "MDFhMGExOTktMDdkNS03N2M4LTljZWUtOTczNTdjMGRkMDhhOjAxYTBhMTk5LTA3ZDUtNzdjOC05Y2VlLTliYjQ2YWVjNGQ4OA=="; 
            string apiKeyPublica = "01a0a199-07d5-77c8-9cee-97357c0dd08a"; 

            Console.WriteLine("Prueba de Cobro");
            Console.Write("Monto a cobrar: $");
            string entrada = Console.ReadLine();

            if (!decimal.TryParse(entrada, out decimal monto) || monto <= 0)
            {
                Console.WriteLine("Monto inválido.");
                Console.ReadKey();
                return;
            }

            var service = new PaywayQrService(apiKeyBase64, apiKeyPublica);
            
            try
            {
                Console.WriteLine($"\nEnviando pago de ${monto:0.00}");
                string paymentId = await service.EnviarCobroAsync(monto);

                Console.WriteLine($"\nCorrecto");
                Console.WriteLine($"Payment ID: {paymentId}");
                Console.WriteLine("Orden de prueba aceptada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR: {ex.Message}"); // muestro error(401: error de autenticación o de estructura, 500: error de servidor)
            }

            Console.WriteLine("\nPresiona una tecla para salir");
            Console.ReadKey();
        }
    }
}