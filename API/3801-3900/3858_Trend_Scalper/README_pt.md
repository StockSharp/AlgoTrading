# Estratégia de Scalper de Tendências (API/3858)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O **TrendScalperStrategy** é uma conversão C# do MetaTrader 4 consultor especialista `Currencyprofits_01_1.mq4`. O robô original é um scalper leve que segue tendências que combina um filtro cruzado EMA/SMA de curto prazo com entradas de breakout em torno dos máximos e mínimos de oscilação mais recentes. A porta StockSharp mantém as mesmas regras de decisão enquanto adota a assinatura de vela de alto nível e o pipeline de indicadores da estrutura.

## Lógica de negociação
1. **Indicadores**
   - EMA rápida (padrão 6) em preços de fechamento.
   - Lento SMA (padrão 12) em preços de fechamento.
   - Máximo mais alto (janela padrão 6) e Mínimo mais baixo (janela padrão 6) calculados a partir dos máximos e mínimos da vela.
2. **Condições de entrada**
   - **Longo**: o preço atinge a banda baixa recente (`Lowest Low`) enquanto o EMA rápida está acima do lento SMA. A estratégia envia uma ordem de compra a mercado com o volume definido pela regra de gestão de dinheiro.
   - **Venda**: o preço toca a banda alta recente (`Highest High`) enquanto a rápida EMA está abaixo da lenta SMA. Uma ordem de venda a mercado é colocada usando o mesmo cálculo de volume.
   - O sistema permanece estável enquanto uma posição está aberta, refletindo o comportamento de ordem única da versão MQL.
3. **Exit Conditions**
   - **Saída longa**: quando uma posição longa aberta vê a máxima da vela quebrar o registrado `Highest High`, a posição é fechada no mercado.
   - **Saída curta**: quando uma posição curta aberta observa a queda da mínima da vela através do `Lowest Low`, a venda é coberta no mercado.
   - Um stop-loss protetor gerenciado por `StartProtection` é anexado a cada negociação quando `StopLossPoints` é maior que zero.

## Gestão de capital
A lógica de dimensionamento de lote reproduz os três modos expostos no script MQL:

| Modo | Descrição | Comportamento no porto |
|------|-------------|-----------------------|
| `0`  | Lotes fixos (`LotsIfNoMM`). | Retorna o `FixedVolume` configurado. |
| `<0` | Lotes fracionários calculados a partir do saldo da conta e do fator de risco. | Calcula `ceil(balance * risk / 10000) / 10`, limitado a 100 lotes. |
| `>0` | Dimensionamento total a partir do equilíbrio e do fator de risco. | Usa a mesma fórmula base, mas o resultado é arredondado para o próximo número inteiro, com limite mínimo de 1 lote e limite de 100. |

O saldo é retirado de `Portfolio.CurrentValue` (voltando para `BeginValue`). Caso o valor da carteira não esteja disponível, a estratégia reverte para o volume fixo para que as ordens ainda sejam emitidas durante os backtests.

## Gestão de risco
- **Stop-loss**: o parâmetro `StopLossPoints` é expresso em pontos de preço (pips). Durante `OnStarted` a distância é multiplicada por `Security.PriceStep` e passada para `StartProtection`, permitindo que StockSharp mantenha a ordem de proteção.
- **Posição única**: a lógica aplica `Position == 0` antes de abrir uma nova negociação, evitando posições sobrepostas exatamente como o especialista MT4.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `CandleType` | Período de 15 minutos | Série de velas usada para cálculos de indicadores e sinais. |
| `FastLength` | 6 | Período do EMA rápida. |
| `SlowLength` | 12 | Período da lentidão SMA. |
| `BreakoutWindow` | 6 | Número de velas inspecionadas para o filtro de fuga mais alto/mais baixo. |
| `FixedVolume` | 0,1 lote | Volume quando o gerenciamento de dinheiro está desativado ou é necessário um substituto. |
| `MoneyManagementMode` | 0 | Seleciona entre tamanho de lote fixo, fracionário ou arredondado. |
| `MoneyManagementRisk` | 40 | Multiplicador de fator de risco usado no dimensionamento de lote baseado em saldo. |
| `StopLossPoints` | 50 | Distância de stop-loss em pontos de preço (convertido em preço absoluto antes de chamar `StartProtection`). |

## Notas de implementação
- O encadeamento de indicadores depende do fluxo de trabalho de alto nível `SubscribeCandles().Bind(...)`; nenhum buffer de série manual é necessário.
- Comentários no código foram adicionados em inglês para corresponder às diretrizes do repositório.
- Nenhum teste unitário foi modificado; o foco desta conversão é a estratégia e a documentação que a acompanha.

## Dicas de uso
- Selecione um intervalo de velas que corresponda ao ambiente de negociação original (por exemplo, prazos intradiários curtos para scalping).
- Certifique-se de que o portfólio tenha um `PriceStep` válido para que a conversão de stop loss em preço absoluto funcione corretamente.
- Ajuste `MoneyManagementRisk` com cuidado: valores mais altos levam a posições maiores devido ao cálculo `ceil(balance * risk / 10000)` herdado do especialista MQL.
