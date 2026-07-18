# Estratégia de Rompimento MA Martingale (ID 2861)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Martingale MA Breakout é uma portação do assessor especialista original do MetaTrader 5 `Martingale.mq5`. Monitora o quão longe o preço atual se afasta de uma média móvel plotada em um timeframe superior. Quando a distância excede um número configurável de pips, a estratégia abre uma nova posição na direção do movimento e a gerencia com lógica fixa de stop-loss, take-profit e trailing. O dimensionamento da posição segue um ajuste estilo martingale que aumenta o tamanho da operação após sequências perdedoras e o reduz após períodos lucrativos.

Por padrão a estratégia avalia velas de 6 minutos enquanto a plataforma circundante pode operar em qualquer timeframe base. Todos os cálculos de indicadores são realizados no tipo de vela selecionado, enquanto as ordens são enviadas usando execução de mercado.

## Lógica de trading

1. Calcular o valor da média móvel para a vela atual usando o método de suavização, preço aplicado e deslocamento selecionados.
2. Transformar a distância configurada em pips em um delta de preço absoluto. O tamanho do pip replica o ajuste original do MQL: símbolos com 3 ou 5 casas decimais multiplicam o passo de preço por 10.
3. Quando a vela fecha:
   - Se o fechamento estiver mais de `DistanceFromMaPips` pips acima da média móvel deslocada e não houver exposição longa ativa, enviar uma ordem de compra de mercado.
   - Se o fechamento estiver mais de `DistanceFromMaPips` pips abaixo da média móvel deslocada e não houver exposição curta ativa, enviar uma ordem de venda de mercado.
4. Cada vela terminada também atualiza o trailing stop e verifica se o preço de fechamento viola o stop-loss ou take-profit simulado. Fechar uma posição aciona `ResetTradeState`, limpando todos os níveis armazenados.

## Gestão de capital

- `RiskPercent` converte em um orçamento de risco monetário usando `Portfolio.CurrentValue` (ou `BeginValue` se nenhuma operação foi realizada). Quando um stop-loss é especificado, o orçamento dividido pela distância do stop e o multiplicador de segurança estima o volume máximo acessível.
- Após o dimensionamento por risco, o volume passa por `ApplyMartingale`: se o último saldo registrado (capturado após a entrada anterior) é maior que o saldo atual, o volume aumenta em 1 unidade; se for menor, o volume diminui em 1 unidade mas nunca cai abaixo do volume base da estratégia.
- A lógica de trailing imita o EA original: uma vez que o preço se move por `TrailingStopPips + TrailingStepPips` a favor da posição, o stop é puxado para manter o offset de `TrailingStopPips`. A estratégia valida que `TrailingStepPips` seja diferente de zero quando o trailing está habilitado, refletindo o tratamento de erros do MQL.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `StopLossPips` | Distância de stop-loss expressa em pips. Um valor de zero desativa o stop e o dimensionamento baseado em risco. |
| `TakeProfitPips` | Distância de take-profit em pips. Zero para desativar. |
| `TrailingStopPips` | Offset do trailing stop em pips. Deve ser combinado com `TrailingStepPips`. |
| `TrailingStepPips` | Movimento de preço adicional necessário antes de o trailing stop avançar. Não pode ser zero quando o trailing está ativo. |
| `DistanceFromMaPips` | Distância mínima entre o preço e a média móvel deslocada que aciona entradas. |
| `CandleType` | Tipo de dados para cálculos de indicadores (padrão timeframe de 6 minutos). |
| `MaPeriod` | Período da média móvel. |
| `MaShift` | Número de barras que a média móvel está deslocada para frente. A estratégia armazena valores históricos de MA para emular o comportamento do MQL. |
| `MaMethod` | Tipo de suavização da média móvel: Simples, Exponencial, Suavizado ou Ponderado. |
| `MaAppliedPrice` | Preço de vela usado para a média móvel (fechamento, abertura, máximo, mínimo, mediano, típico ou ponderado). |
| `RiskPercent` | Percentagem do capital atual alocada ao orçamento de risco do stop-loss. |

## Notas de execução

- A estratégia funciona exclusivamente em velas terminadas para replicar o processamento de "nova barra" do EA original. `BuyMarket`/`SellMarket` virará a exposição existente adicionando o valor absoluto da posição oposta.
- Os stops e alvos são simulados em código porque o StockSharp não os gerencia automaticamente nesta conversão. O preço de fechamento é usado como proxy para execução no nível de tick.
- Os ajustes de martingale operam no snapshot do saldo da conta tomado imediatamente após cada entrada, semelhante ao EA fonte.
- Se o símbolo carecer de um passo de preço ou multiplicador válido, valores padrão de `0.0001` e `1` são usados para evitar erros de divisão.

## Diferenças do EA original

- A versão MQL usava preços bid/ask; esta portação trabalha com preços de fechamento de velas porque ticks de alta frequência não estão disponíveis na API de alto nível.
- O dimensionamento de volume depende do capital do portfólio e do multiplicador de segurança em vez do helper `CMoneyFixedMargin`.
- A visualização gráfica é opcional: quando uma área de gráfico está disponível, a estratégia desenha velas; nenhum indicador adicional é plotado por padrão.
- A validação de que `TrailingStepPips` deve ser positivo quando o trailing está habilitado lança uma exceção durante a inicialização em vez de chamar `Alert`.
