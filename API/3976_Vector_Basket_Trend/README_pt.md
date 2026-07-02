# Estratégia de tendência de cesta vetorial (porta MT4)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta pasta contém a porta StockSharp de alto nível API do consultor especialista MetaTrader 4 **Vector** (script original: `MQL/8305/Vector.mq4`). A estratégia coordena até quatro pares forex principais – EURUSD (primário), GBPUSD, USDCHF e USDJPY – e os negocia na mesma direção quando aparece um alinhamento de média móvel suavizada compartilhada. A conversão mantém as ideias principais do Vector enquanto as adapta aos padrões idiomáticos StockSharp.

## Lógica de negociação

1. **Médias móveis suavizadas (SMMA)** – cada instrumento rastreia uma SMMA rápida (3 períodos) e lenta (7 períodos) calculada com base nos preços médios do período de negociação configurável (15 minutos por padrão).
2. **Filtro de tendência vetorial** – as diferenças entre cada par rápido/lento são somadas. Uma soma positiva sinaliza uma dinâmica de alta sincronizada em toda a cesta, enquanto uma soma negativa implica uma pressão coletiva de baixa.
3. **Regras de entrada** – a estratégia abre ou reverte posições com ordens de mercado somente quando:
   - A tendência da cesta é positiva e o SMMA rápido do instrumento permanece acima do SMMA lento (entrada longa).
   - A tendência da cesta é negativa e o SMMA rápido está abaixo do SMMA lento (entrada curta).
4. **Meta de pip da faixa H4** – para cada instrumento, uma assinatura de vela separada de 4 horas mede a faixa anterior. Um quinto desse intervalo (limitado a 13 pips) torna-se o objetivo de lucro por posição, refletindo a saída de pip fixo do código MT4.
5. **Global equity guard** – limites de lucro e rebaixamento baseados em porcentagem (retirados das entradas originais `PrcProfit` e `PrcLose`) fecham todas as posições abertas uma vez acionadas.

## Principais diferenças em relação ao original EA

- As **assinaturas de velas de alto nível e vinculação de indicadores** de StockSharp substituem a pesquisa de baixo nível encontrada no MT4 (`SubscribeCandles().Bind(...)`).
- A porta suporta **instrumentos secundários opcionais**: deixe os slots GBPUSD / USDCHF / USDJPY vazios para negociar apenas o título principal.
- O tamanho dinâmico do lote vinculado à margem da conta MT4 foi substituído por um parâmetro `BaseVolume` limpo que é normalizado para `VolumeStep`, `MinVolume` e `MaxVolume` de cada título.
- O gerenciamento comercial armazena preços de entrada por meio de retornos de chamada `OnNewMyTrade`, evitando pesquisas diretas de valores de indicadores não permitidas.

## Parâmetros

| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromMinutes(15)` | Prazo utilizado para os cálculos do SMMA e verificações de entrada. |
| `RangeCandleType` | `TimeSpan.FromHours(4)` | Período de tempo mais alto usado para derivar o alvo do pip adaptativo. |
| `SecondSecurity` | `null` | Slot GBPUSD opcional (defina um `Security` antes de começar). |
| `ThirdSecurity` | `null` | Slot USDCHF opcional. |
| `FourthSecurity` | `null` | Slot USDJPY opcional. |
| `BaseVolume` | `1` | Volume de negociação solicitado por pedido, normalizado de acordo com os limites cambiais. |
| `TakeProfitPercent` | `0.5` | Ganho de capital global (em %) que desencadeia uma saída de todo o portfólio. |
| `MaxDrawdownPercent` | `30` | Redução máxima permitida do patrimônio (em%) antes do fechamento de todas as posições. |

## Notas de uso

- Atribua o mesmo conector e portfólio a todos os títulos referenciados pelos parâmetros antes de iniciar a estratégia.
- Certifique-se de que a fonte de dados forneça o período de negociação e o intervalo de tempo para todos os instrumentos.
- Quando os títulos facultativos não são fornecidos, o cálculo vetorial se adapta automaticamente aos instrumentos disponíveis.
- As saídas sempre acontecem com ordens de mercado para corresponder ao comportamento original do MT4.

## Arquivos

- `CS/VectorStrategy.cs` – implementação de C# seguindo as diretrizes de alto nível StockSharp.
- `README.md`, `README_ru.md`, `README_zh.md` – documentação de estratégia em inglês, russo e chinês.
