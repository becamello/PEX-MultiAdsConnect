namespace MultiAdsConnect.Models;

public class CampaignReport
{
    public string Campanha { get; set; } = "";
    public string Periodo { get; set; } = "";
    public int Alcance { get; set; }
    public int Cliques { get; set; }
    public int Conversoes { get; set; }
    public decimal Custo { get; set; }
    public decimal Cpc { get; set; }
    public decimal Cpm { get; set; }
    public string Observacoes { get; set; } = "";
}
