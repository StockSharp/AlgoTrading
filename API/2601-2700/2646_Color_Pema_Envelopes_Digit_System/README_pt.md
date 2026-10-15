# Color PEMA Envelopes Sistema Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O **Color PEMA Envelopes Sistema Digit** reproduz a lógica do especialista MetaTrader
`Exp_Color_PEMA_Envelopes_Digit_System.mq5`. A estratégia avalia os códigos de cor
produzidos pelo indicador Color PEMA Envelopes: quando uma vela fecha fora da
banda superior ou inferior, o indicador pinta uma cor especial, e quando o preço
volta a entrar no canal, uma operação é acionada na direção do rompimento.

## Como funciona
1. A estratégia constrói uma EMA Polinomial de oito estágios (PEMA) usando comprimentos fracionários,
   exatamente como no indicador original. O resultado é arredondado para a precisão configurada
   e deslocado pelo deslocamento de preço opcional.
2. Envelopes superior e inferior são criados aplicando um desvio percentual ao redor do valor PEMA.
3. Cada vela finalizada recebe um código de cor dependendo de sua relação com os envelopes deslocados:
   - `4`/`3`: fechamento acima da banda superior (corpo de alta/baixa).
   - `1`/`0`: fechamento abaixo da banda inferior (corpo de alta/baixa).
   - `2`: o preço permanece dentro do envelope.
4. A estratégia lê a cor que ocorreu na vela `SignalBar + 1` e a compara com
a cor da vela `SignalBar`. Isso imita as chamadas `CopyBuffer` do consultor especialista.
5. Quando a cor mais antiga indica um rompimento acima da banda superior e a cor mais recente
retorna para dentro do canal, uma entrada comprada é permitida (se habilitada) e qualquer posição vendida é fechada.
   A lógica espelhada é usada para entradas vendidas e para fechar posições compradas.
6. Ordens de stop-loss e take-profit protetoras são gerenciadas através do módulo de risco do StockSharp.

## Parâmetros
- `CandleType` – período usado para análise e trading.
- `TradeVolume` – quantidade enviada com ordens de mercado.
- `EmaLength` – comprimento fracionário usado por cada camada EMA na cadeia PEMA.
- `AppliedPrices` – preço fonte (fechamento, abertura, mediano, ponderado, seguidor de tendência, DeMark, etc.).
- `DeviationPercent` – distância percentual para ambos os envelopes ao redor de PEMA.
- `Shift` – número de velas completadas usadas para deslocar a comparação do envelope.
- `PriceShift` – deslocamento absoluto adicional aplicado a ambos os envelopes.
- `Digit` – dígitos de precisão extras ao arredondar a saída PEMA.
- `SignalBar` – quantas velas fechadas atrás ler a cor atual (a cor mais antiga é tomada uma barra mais atrás).
- `AllowBuyOpen` / `AllowSellOpen` – habilitar ou desabilitar novas entradas compradas/vendidas.
- `AllowBuyClose` / `AllowSellClose` – permitir fechar posições compradas/vendidas em sinais opostos.
- `StopLossPoints` – distância de stop protetor em pontos de preço (multiplicado por `PriceStep`).
- `TakeProfitPoints` – distância do alvo de lucro em pontos de preço.

## Valores padrão
- `CandleType = TimeSpan.FromHours(4).TimeFrame()`
- `TradeVolume = 1m`
- `EmaLength = 50.01m`
- `AppliedPrices = AppliedPrices.Close`
- `DeviationPercent = 0.1m`
- `Shift = 1`
- `PriceShift = 0m`
- `Digit = 2`
- `SignalBar = 1`
- `AllowBuyOpen = true`
- `AllowSellOpen = true`
- `AllowBuyClose = true`
- `AllowSellClose = true`
- `StopLossPoints = 1000m`
- `TakeProfitPoints = 2000m`

## Filtros
- **Categoria**: Rompimento / Re-entrada no canal
- **Direção**: Comprado/Vendido
- **Indicadores**: Envelopes de EMA Polinomial
- **Stops**: Sim (stop-loss e take-profit baseados em pontos)
- **Período**: Swing (padrão 4H)
- **Nível de risco**: Moderado – opera apenas quando o preço retorna de um extremo
- **Sazonalidade**: Nenhuma
- **Redes neurais**: Não
- **Divergência**: Não
