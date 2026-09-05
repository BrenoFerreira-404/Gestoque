namespace Gestoque.Domain.Enums;

public enum MovementType
{
    Entrada = 1,
    Saida = 2,
    Ajuste = 3
}

public enum MovementReason
{
    CompraFornecedor = 1,
    DoacaoRecebida = 2,
    ConsumoCozinha = 3,
    DescarteVencido = 4,
    AvariaDanificado = 5,
    AjusteInventario = 6,
    Outros = 99
}

public enum UnitOfMeasure
{
    UND = 1,
    KG = 2,
    G = 3,
    L = 4,
    ML = 5,
    CX = 6,
    FD = 7,
    LATA = 8,
    PACOTE = 9
}

public enum ExpiryStatus
{
    Normal = 1,         // > 60 dias (Verde)
    ProximoVencimento = 2, // <= 30 dias (Amarelo / Laranja)
    Critico = 3,        // <= 7 dias (Amarelo forte / Laranja)
    Vencido = 4         // <= 0 dias (Vermelho)
}

