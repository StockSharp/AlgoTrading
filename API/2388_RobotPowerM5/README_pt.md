# Estratégia RobotPower M5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina os indicadores Bulls Power e Bears Power em um gráfico de 5 minutos.
Abre posições quando o momentum combinado de touros e ursos cruza zero e gerencia saídas com alvos fixos e um trailing stop.

## Como funciona
- **Indicadores**: Bulls Power e Bears Power com um período compartilhado `BullBearPeriod`.
- **Período**: velas de 5 minutos por padrão (`CandleType`).

### Regras de entrada
- **Entrada comprada**: Quando `BullsPower + BearsPower > 0` e nenhuma posição está aberta, comprar a mercado.
- **Entrada vendida**: Quando `BullsPower + BearsPower < 0` e nenhuma posição está aberta, vender a mercado.

### Regras de saída
- **Take Profit**: Fechar a posição quando o preço se mover `TakeProfit` unidades na direção da operação.
- **Stop Loss**: Fechar a posição se o preço se mover contra a posição por `StopLoss` unidades.
- **Trailing Stop**: Após a entrada, o stop loss segue o preço por `TrailingStep` uma vez que o preço avança mais que o dobro dessa distância.

### Parâmetros
- `BullBearPeriod` – período para os cálculos de Bulls Power e Bears Power.
- `TrailingStep` – tamanho do passo ao ajustar o trailing stop.
- `TakeProfit` – distância da entrada até o nível de take profit.
- `StopLoss` – distância da entrada até o nível de stop loss.
- `CandleType` – período dos candles para cálculo de sinais.

### Tamanho da posição
Usa a propriedade `Volume` da estratégia para o tamanho da ordem.

## Notas
Projetada para fins educacionais e serve como exemplo de conversão de uma estratégia MQL para a API do StockSharp.
