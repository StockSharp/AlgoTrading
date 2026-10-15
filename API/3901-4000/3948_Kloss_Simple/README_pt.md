# Estratégia Simples Kloss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Kloss Simple Strategy** é uma conversão direta do MetaTrader 4 consultor especialista `Kloss_.mq4`. Ele reconstrói a ideia de negociação original usando StockSharp de alto nível API e mantém o conjunto de indicadores idêntico: uma média móvel exponencial (EMA) calculada sobre preços de fechamento ponderados, o índice de canal de commodities (CCI) e o oscilador Stochastic. Os sinais são gerados a partir da vela concluída anteriormente, espelhando a lógica de mudança de uma barra na versão MQL. O dimensionamento da posição pode basear-se num volume fixo de ordens ou numa percentagem de risco do valor da carteira, tal como as regras originais de cálculo do lote.

## Ideia Central

1. Monitore o contexto de impulso com limites **CCI** e **Stochastic** em torno de seus níveis neutros.
2. Confirme os sinais de impulso com um **EMA** de curto prazo do preço de fechamento ponderado.
3. Insira posições somente quando a vela anterior satisfizer todas as condições de sinal, evitando negociações prematuras com dados de mercado incompletos.
4. Permitir múltiplas entradas na mesma direção até um limite configurável, emulando o parâmetro "MaxOrders" do script MT4.

## Configuração do Indicador

- **EMA (MaPeriod)**: usa o fechamento ponderado `(Close * 2 + High + Low) / 4` para corresponder a `PRICE_WEIGHTED` de MetaTrader. Atua como um filtro de tendências de curto prazo.
- **CCI (CciPeriod)**: Avalia desvios de momentum em relação ao preço médio. Limite `±CciLevel` define entradas agressivas versus conservadoras.
- **Stochastic (StochasticKPeriod / DPeriod / Smooth)**: Usa a linha %K principal para detectar condições de sobrecompra ou sobrevenda em relação ao nível neutro 50. O desvio de 50 é controlado por `StochasticLevel`.

Todos os indicadores operam na série de velas primárias definida por `CandleType`. A estratégia atualiza os valores dos indicadores apenas nas velas finalizadas, garantindo backtesting estável e comportamento ao vivo.

## Lógica de negociação

### Configuração longa

1. O fechamento da vela anterior está acima do valor anterior de EMA.
2. O valor anterior de CCI está abaixo de `-CciLevel`, sinalizando impulso de sobrevenda.
3. O valor anterior de Stochastic %K está abaixo de `50 - StochasticLevel`, confirmando oscilação de sobrevenda.
4. Quando as condições se mantêm, qualquer exposição curta é fechada e uma nova posição longa é aberta, desde que o número de ordens longas existentes seja inferior a `MaxOrders`.

### Configuração curta

1. O fechamento da vela anterior está abaixo do valor anterior de EMA.
2. O valor anterior de CCI está acima de `+CciLevel`, sinalizando impulso de sobrecompra.
3. O valor anterior de Stochastic %K está acima de `50 + StochasticLevel`, confirmando a oscilação de sobrecompra.
4. Quando as condições se mantêm, qualquer exposição longa é fechada e uma nova posição curta é aberta, sujeita ao limite `MaxOrders`.

### Gerenciamento de saída

- **Stop Loss / Take Profit**: Distâncias absolutas opcionais em pontos do instrumento. Se um dos valores for maior que zero, a proteção de posição integrada do StockSharp será ativada.
- **Sinal Oposto**: Antes de abrir na direção oposta, a posição atual é fechada para imitar o consultor especialista original.

## Dimensionamento de posições

- **OrderVolume**: tamanho fixo padrão que replica o parâmetro `Lots` do MT4.
- **RiskPercentage**: Quando maior que zero, a estratégia calcula o tamanho da negociação como uma porcentagem do valor do portfólio. Ele usa requisitos de margem de instrumento quando disponíveis, caso contrário, recorre ao dimensionamento baseado em preço, reproduzindo o comportamento `Lots == 0` do código MQL.
- **MaxOrders**: limita o volume cumulativo por direção, permitindo até `MaxOrders * OrderVolume` exposição.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `OrderVolume` | Tamanho base do pedido usado quando `RiskPercentage` é zero. |
| `MaPeriod` | Comprimento do EMA baseado nos preços de fechamento ponderados. |
| `CciPeriod` | Número de barras usadas no cálculo CCI. |
| `CciLevel` | Limite absoluto CCI para geração de sinal. |
| `StochasticKPeriod` | Lookback para a linha Stochastic %K. |
| `StochasticDPeriod` | Período médio móvel para a linha %D. |
| `StochasticSmooth` | Suavização adicional aplicada a %K. |
| `StochasticLevel` | Desvio de 50 usado para detecção de sobrecompra/sobrevenda. |
| `MaxOrders` | Número máximo de entradas permitidas por direção. |
| `StopLossPoints` | Distância de stop loss opcional em faixas de preço. |
| `TakeProfitPoints` | Distância de lucro opcional em faixas de preço. |
| `RiskPercentage` | Porcentagem do portfólio para dimensionamento dinâmico de posição. |
| `CandleType` | Série de velas usada para todos os cálculos. |

## Notas práticas

- Funciona melhor em dados intradiários, onde os osciladores de curto prazo reagem rapidamente às oscilações de preços.
- O preço de fechamento ponderado mantém o EMA responsivo enquanto ainda incorpora a faixa máxima/mínima da vela.
- Como toda decisão depende da vela anterior, a estratégia evita a repintura intra-barra e permanece determinística em testes históricos.
- A gestão de risco deve estar alinhada com as especificações do contrato da corretora para que `OrderVolume` e `MaxOrders` correspondam aos tamanhos de negociação executáveis.
