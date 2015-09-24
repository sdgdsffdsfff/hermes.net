
namespace cmessaging.consumer
{
    internal class Util
    {
        public static string GetLatencyDistribution(double val)
        {
            if (val < 10)
            {
                return "0~10ms";
            }
            else if (val >= 10 && val < 50)
            {
                return "10~50ms";
            }
            else if (val >= 50 && val < 200)
            {
                return "50~200ms";
            }
            else if (val >= 200 && val < 500)
            {
                return "200~500ms";
            }
            else if (val >= 500 && val < 1000)
            {
                return "500~1s";
            }
            else if (val >= 1000 && val < 5000)
            {
                return "1~5s";
            }
            else if (val >= 5000 && val < 10000)
            {
                return "5~10s";
            }
            else if (val >= 10000 && val < 30000)
            {
                return "10~30s";
            }
            else if (val >= 30000 && val < 100000)
            {
                return "30~100s";
            }
            else
            {
                return ">100s";
            }
        }

        public static string GetMessageSizeDistribution(double val)
        {
            val = val / 1024;
            if (val < 10)
            {
                return "0~10kb";
            }
            else if (val >= 10 && val < 50)
            {
                return "10~50kb";
            }
            else if (val >= 50 && val < 200)
            {
                return "50~200kb";
            }
            else if (val >= 200 && val < 500)
            {
                return "200~500kb";
            }
            else if (val >= 500 && val < 1024)
            {
                return "500~1024kb";
            }
            else if (val >= 1024 && val < 1536)
            {
                return "1~1.5m";
            }
            else if (val >= 1536 && val < 2048)
            {
                return "1.5~2m";
            }
            else
            {
                return ">2m";
            }
        }

        public static string GetMessageLatencyDistribution(double val)
        {
            if(val<=1){
                return "0~1s";
            }
            else if(val>1 && val<=3){
                return "1~3s";
            }
            else if(val>3 && val<=5){
                return "3~5s";
            }
            else if(val>5 && val<=10){
                return "5~10s";
            }
            else if(val>10 && val<=30){
                return "10~30s";
            }
            else if(val>30 && val<=60){
                return "30~60s";
            }
            else if(val>60 && val<=100){
                return "60~100s";
            }
            else{
                return ">100s";
            }
        }
    }
}
