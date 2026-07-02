# Estratégia Auto Trading Publish
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia porta o utilitário **"Auto Trading Publish"** do MetaTrader 4 para o StockSharp. Em vez de enviar ordens a mercado, ela se concentra em controlar quando a negociação é permitida. Ela monitora o relógio do mercado por uma assinatura de candles e alterna a flag `AutoTradingActive` quando a hora configurada de início ou parada é atingida. A flag espelha o comportamento do utilitário original, que alternava programaticamente o botão "AutoTrading" do MT4.

## Lógica de negociação
- Assinar um fluxo leve de candles (candles de um minuto por padrão) para acompanhar a hora de mercado mesmo quando nenhuma operação é feita.
- Quando um candle concluído informa a `StartHour` configurada, habilitar a flag `AutoTradingActive` e registrar o evento.
- Quando um candle concluído informa a `StopHour` configurada, desabilitar a flag `AutoTradingActive` e registrar o evento.
- Suprimir alternâncias duplicadas dentro da mesma hora para que o log não seja inundado quando vários candles ou ticks chegarem durante aquela hora.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `StartHour` | Hora do dia (0-23) que habilita a negociação automática. |
| `StopHour` | Hora do dia (0-23) que desabilita a negociação automática. |
| `CandleType` | Timeframe usado para consultar o relógio de mercado. Frames menores reagem mais rápido. |

## Notas de uso
- A estratégia não envia ordens; ela apenas expõe a propriedade `AutoTradingActive`, que outras estratégias ou painéis de controle podem observar para decidir quando enviar operações.
- Quando a hora de início e parada são iguais, o evento de parada roda após o evento de início, deixando a negociação desabilitada, idêntico ao expert advisor original.
- Escolha um timeframe de candles que corresponda à rapidez necessária para a alternância. Um timeframe de um minuto é um bom equilíbrio entre responsividade e uso de recursos.

## Diferenças em relação ao MetaTrader
- O MT4 alternava um botão global da plataforma por mensagens do Windows. O StockSharp expõe uma flag em nível de estratégia, tornando o comportamento mais fácil de integrar com setups complexos.
- O port StockSharp roda inteiramente dentro da API de alto nível, facilitando a combinação com gráficos ou outras estratégias auxiliares sem hooks de mensagens de baixo nível.
