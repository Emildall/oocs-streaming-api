using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Http;

namespace oocs_streaming_api.Controllers
{
    public class StreamController : ApiController
    {

        Socket hostSocket;
        Thread thread;
        string localip = "";
        string computerHostName = "";
        private Bitmap frame = null;
        private static Stream _stream;
        private string BOUNDARY = "frame";
        public StreamController()
        {

        }
        [Authorize]
        [Route("Api/Stream/PostStream")]
        [System.Web.Mvc.HttpPost]
        public HttpResponseMessage PostStream([FromBody]byte[] b)
        {
            ReceiveImage(b);
            //computerHostName = Dns.GetHostName();
            //IPHostEntry hostname = Dns.GetHostEntry(computerHostName);
            //foreach (var ip in hostname.AddressList)
            //{
            //    if (ip.AddressFamily.ToString() == "InterNetwork")
            //    {
            //        localip = ip.ToString();
            //    }
            //} 
            //ConnectSocket();

            return new HttpResponseMessage(HttpStatusCode.OK);
        }


        private void ConnectSocket()
        {
            TcpClient client = null;
            TcpListener server = null;
            try
            {
                IPAddress localIpAdr = IPAddress.Parse(localip);
                server = new TcpListener(localIpAdr, 104);
                server.Start();

                //IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, 104);
                client = server.AcceptTcpClient();

                bool done = false;
                var b = new byte[1280 * 720];

                //Socket receiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Udp);
                //IPEndPoint hostIp = new IPEndPoint(IPAddress.Parse(localip), 104);
                //receiveSocket.Bind(hostIp);
                //receiveSocket.Listen(10);
                try
                {
                    while (!done)
                    {
                        NetworkStream stream = client.GetStream();
                        int bytes = stream.Read(b, 0, b.Length);

                        ReceiveImage(b);
                    }
                    //hostSocket = receiveSocket.Accept();
                    //thread = new Thread(new ThreadStart(ReceiveImage));
                    //thread.IsBackground = true;
                    //thread.Start();
                }
                catch (Exception e)
                {

                    throw;
                }

                return;
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                if (client != null)
                    client.Close();

                if (server != null)
                    server.Stop();
            }


        }

        private void ReceiveImage(byte[] b)
        {
            int dataSize;
            string imageName = "image-" + DateTime.Now.Ticks + "Png"; ;
            try
            {
                dataSize = 0;
                //byte[] b = new byte[1024 * 10000];
                dataSize = b.Length;
                if (dataSize > 0)
                {

                    WriteFrame(_stream, b);
                }
            }
            catch (Exception)
            {

                return;
            }

            //Thread.Sleep(30);
            //ReceiveImage();
        }


        [System.Web.Mvc.HttpGet]
        public HttpResponseMessage GetStream()
        {
            // start the video source


            var response = Request.CreateResponse();

            response.Content = new PushStreamContent((Action<Stream, HttpContent, TransportContext>)StartStream);
            response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("multipart/x-mixed-replace; boundary=" + BOUNDARY);
            return response;
        }
        private byte[] CreateHeader(int length)
        {
            string header =
                "--" + BOUNDARY + "\r\n" +
                "Content-Type:image/png\r\n" +
                "Content-Length:" + length + "\r\n\r\n";

            return Encoding.ASCII.GetBytes(header);
        }

        public byte[] CreateFooter()
        {
            return Encoding.ASCII.GetBytes("\r\n");
        }
        private void WriteFrame(Stream stream, byte[] imageData)
        {
            // prepare image data
            try
            {
                if (stream == null) return;

                var imgdata =Decompress(imageData);

                // prepare header
                byte[] header = CreateHeader(imgdata.Length);
                // prepare footer
                byte[] footer = CreateFooter();

                // Start writing data
                stream.Write(header, 0, header.Length);
                stream.Write(imgdata, 0, imgdata.Length);
                stream.Write(footer, 0, footer.Length);
            }
            catch (Exception e)
            {
                //Console.log("Error in writeframe : " + e.Message());
                return;
            }

        }

        public static byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }

        private void StartStream(Stream stream, HttpContent httpContent, TransportContext transportContext)
        {

            _stream = stream;

        }
    }
}
