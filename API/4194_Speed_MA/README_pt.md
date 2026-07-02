# Estratégia MA de velocidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Speed MA** é uma porta StockSharp direta do MetaTrader 4 consultor especialista `ytg_Speed_MA_ea`. O sistema original mede a rapidez com que uma média móvel simples muda de uma barra para outra. Quando a inclinação da média móvel excede um limite definido pelo usuário, o especialista abre uma posição de mercado na direção correspondente. Esta implementação C# reproduz esse comportamento com o API de alto nível de StockSharp: ela assina velas, avalia uma média móvel simples deslocada e aciona negociações quando a diferença entre valores deslocados consecutivos é grande o suficiente. A estratégia mantém o volume do pedido, as metas de lucro e os stop loss expressos em MetaTrader "pontos" para permanecer fiel ao código-fonte.

## Lógica de negociação
1. Assine o tipo de vela configurado (velas de um minuto por padrão) e crie uma média móvel simples usando o parâmetro `MovingAveragePeriod`.
2. Para cada vela finalizada, registre o valor da média móvel mais recente. A lista de histórico mantém apenas os valores necessários para avaliar o `Shift` configurado e a barra anterior antes dele.
3. Calcule a inclinação como a diferença entre o valor da média móvel `Shift` barras atrás e o valor uma barra antes (ou seja, `Shift + 1` barras atrás). Isso reflete a chamada MetaTrader `iMA(..., shift)` e `iMA(..., shift + 1)`.
4. Compare a inclinação com `SlopeThresholdPoints` convertida em unidades de preço absoluto. Se a diferença for maior que o limite positivo, gere um sinal longo. Se a diferença for inferior ao limite negativo, gere um sinal curto.
5. Quando `ReverseSignals` estiver ativado, inverta o sinal gerado para que uma inclinação de alta abra uma posição curta e vice-versa.
6. Envie uma nova ordem de mercado somente quando não houver posição ativa. O consultor especialista original confiou em `OrdersTotal() < 1` e nunca reverteu diretamente; esta implementação se comporta de forma idêntica, ignorando os sinais enquanto uma posição está aberta.
7. As ordens de proteção são gerenciadas por meio de `StartProtection`. As distâncias de stop loss e takeprofit são definidas em MetaTrader pontos (`TakeProfitPoints` e `StopLossPoints`) e traduzidas automaticamente em compensações de preço usando a precisão decimal do título.

## Gestão de risco
- **Stop-loss** – `StopLossPoints` define quantos MetaTrader pontos abaixo/acima da entrada o stop de proteção é colocado. Um valor de `0` desativa o stop loss.
- **Take-profit** – `TakeProfitPoints` define a distância da meta de lucro em MetaTrader pontos. A configuração de `0` desativa a meta de lucro.
- A estratégia não faz trail stops nem realiza lucros parciais; ele se concentra em replicar o comportamento original que define imediatamente metas fixas e para quando um pedido é atendido.
- Como o especialista só abre uma nova posição quando está plano, nunca há mais de uma posição ativa. Isso torna o dimensionamento da posição previsível e reflete a implementação MetaTrader onde o volume foi fixado em 0,1 lote.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Volume de negociação usado para entradas no mercado. Equivalente ao tamanho do lote `0.1` do EA original. | `0.1` |
| `MovingAveragePeriod` | Período da média móvel simples usado para medir a velocidade. | `13` |
| `Shift` | Número de barras completas entre a vela atual e a amostra da média móvel. A estratégia compara os valores em `shift` e `shift + 1`. | `1` |
| `SlopeThresholdPoints` | Diferença mínima entre os dois valores da média móvel deslocada, medida em MetaTrader pontos. | `10` |
| `ReverseSignals` | Inverta a direção da negociação para que uma inclinação de alta abra uma posição curta. | `false` |
| `TakeProfitPoints` | Distância de lucro expressa em MetaTrader pontos (convertida internamente em preço absoluto). | `500` |
| `StopLossPoints` | Distância de stop loss expressa em MetaTrader pontos (convertida internamente em preço absoluto). | `490` |
| `CandleType` | Tipo de vela usado para cálculos (o padrão é um período de 1 minuto). | `1 minute` período de tempo |

## Notas de implementação
- A constante `Point` de MetaTrader é reconstruída usando o `Decimals` do instrumento. Para símbolos Forex de 5 ou 3 decimais, o código divide um por `10^Decimals` para obter o mesmo valor de tick usado em MetaTrader.
- O histórico do valor da média móvel é cortado para manter apenas as amostras exigidas pelo `Shift` selecionado. Isso evita o crescimento ilimitado da memória, ao mesmo tempo que respeita os índices exatos referenciados pelo consultor especialista.
- `StartProtection` converte os parâmetros baseados em pontos MetaTrader em StockSharp `Unit` instâncias com compensações de preço absoluto. Isso mantém as distâncias de stop-loss e take-profit idênticas à versão MQL4.
- A estratégia usa o fluxo de trabalho de alto nível `SubscribeCandles().Bind(...)` para que as atualizações dos indicadores e a avaliação do sinal ocorram apenas nas velas finalizadas. Nenhuma chamada manual para `Indicator.GetValue()` é necessária.
- Comentários embutidos em inglês são fornecidos no código-fonte para destacar as decisões críticas de conversão.
- Somente a implementação C# é fornecida. Uma porta Python é omitida intencionalmente, correspondendo à solicitação.

## Dicas de uso
- A redução de `SlopeThresholdPoints` aumenta o número de negociações porque movimentos menores da média móvel se qualificam como sinais. Aumentar o valor filtra mais negociações e exige um impulso mais forte.
- Ajuste `Shift` para alterar quantas barras atrás a inclinação é medida. Um valor de `0` compara a barra finalizada atual com a barra anterior, enquanto valores mais altos avaliam seções mais antigas da média móvel.
- Combine a estratégia com StockSharp módulos de risco ou controles em nível de portfólio se for necessária uma gestão adicional de dinheiro além de limites e metas fixas.
- Certifique-se de que o `CandleType` assinado corresponda ao período usado ao otimizar o especialista MQL4. As diferenças no período de tempo alteram drasticamente a magnitude da inclinação.

## Diferenças em relação ao Expert Advisor original
- As entradas e saídas de mercado usam os auxiliares de ordem de mercado de StockSharp em vez de `OrderSend`, mas o comportamento resultante (uma ordem de mercado com SL/TP fixo) permanece idêntico.
- MetaTrader gerencia pedidos usando contagens de tickets; StockSharp monitora a posição agregada. A lógica que requer uma posição plana antes de abrir uma nova negociação recria `OrdersTotal() < 1` no novo ambiente.
- O registro, a visualização de gráficos e o manuseio de unidades agora aproveitam os recursos do StockSharp, fornecendo melhores diagnósticos sem afetar as decisões comerciais.

## Arquivos
- `CS/SpeedMAStrategy.cs` – implementação de estratégia.
- `README.md`, `README_zh.md`, `README_ru.md` – documentação detalhada em inglês, chinês e russo, respectivamente.

Nenhum diretório Python está incluído, de acordo com as diretrizes de conversão.
