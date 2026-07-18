# EMA Estratégia de risco WMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do consultor especialista MetaTrader 4 "EMA WMA" por Vladimir Hlystov.
- Reversões de tendência de negociação detectadas a partir da relação entre uma média móvel exponencial (EMA) e uma média móvel ponderada (WMA) calculada nos preços de velas **abertas**.
- Anexa automaticamente ordens de stop-loss e take-profit idênticas ao robô MT4 usando o auxiliar de proteção de StockSharp.
- Suporta dimensionamento de posição baseado em risco que reflete a entrada de "risco" original, mantendo ao mesmo tempo uma opção para negociação de volume fixo.

## Lógica Original do Expert Advisor
- A versão MT4 funciona em qualquer símbolo e período de tempo, avaliando os sinais uma vez em uma nova barra (guardada por `TimeBar`).
- Os indicadores usam `PRICE_OPEN`, então as médias reagem ao tick de abertura da barra.
- Quando EMA cai abaixo do WMA enquanto anteriormente estava acima dele, todas as posições curtas são fechadas e uma negociação longa é aberta com distâncias predefinidas de stop-loss e take-profit.
- Quando EMA sobe acima do WMA depois de estar abaixo dele, todas as posições longas são fechadas e uma nova posição curta é aberta.
- A entrada `risk` calcula o tamanho do lote a partir da margem disponível e da distância do stop loss.

## Regras de negociação em StockSharp
1. Assine a série de velas configurada (`CandleType`, padrão 30 minutos). Apenas velas acabadas são processadas para evitar repinturas.
2. Insira os preços de abertura das velas nos indicadores EMA e WMA. Espere até que ambos os indicadores sejam formados.
3. Detecte um cruzamento de alta quando EMA anterior > WMA anterior e EMA atual < WMA atual.
   - Feche todas as posições vendidas e entre em uma posição comprada dimensionada pelas regras de risco.
4. Detecte um cruzamento de baixa quando EMA anterior < WMA anterior e EMA atual > WMA atual.
   - Feche todas as posições compradas e insira uma posição vendida dimensionada pelas regras de risco.
5. `StartProtection` cria ordens de proteção de mercado para que cada nova negociação receba imediatamente níveis de stop-loss e take-profit expressos em etapas de preço.

## Dimensionamento de posição e controle de risco
- **RiskPercent** emula o parâmetro MT4 `risk`. O volume é calculado a partir do patrimônio do portfólio, da distância do stop-loss e dos valores de step/step-price do título.
- Se os metadados da exchange estiverem faltando (nenhuma etapa de preço ou preço de etapa), o algoritmo volta a usar a distância de parada absoluta.
- Se `RiskPercent` for definido como zero, a estratégia exigirá um **OrderVolume** positivo (substituição de volume fixo).
- A exposição oposta existente é fechada antes do envio de novos pedidos, correspondendo ao comportamento MT4 de `CLOSEORDER` e depois de `OPENORDER`.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `EmaPeriod` | Período da média móvel exponencial (padrão 28). |
| `WmaPeriod` | Período da média móvel ponderada (padrão 8). |
| `StopLossPoints` | Distância de stop loss em etapas do instrumento (padrão 50). |
| `TakeProfitPoints` | Distância de take-profit em etapas do instrumento (padrão 50). |
| `RiskPercent` | Porcentagem de patrimônio líquido em relação ao risco por negociação (padrão 10%). |
| `OrderVolume` | Volume fixo de pedidos; use 0 para ativar o dimensionamento baseado em risco. |
| `CandleType` | Tipo de dados/período de vela usado para cálculos. |

## Notas de implementação
- Os valores EMA e WMA são enviados manualmente por meio de `DecimalIndicatorValue` para garantir que o preço de abertura seja usado exatamente como a configuração do indicador MT4.
- A estratégia depende de velas fechadas para confirmação do sinal; isso pode atrasar as entradas em uma barra em comparação com MT4, mas evita viés antecipado.
- As ordens de proteção são expressas em etapas de preço para corresponder ao multiplicador `Point` de MetaTrader.
- Os gráficos traçam velas automaticamente, tanto médias móveis quanto marcadores comerciais quando uma área do gráfico está disponível.
