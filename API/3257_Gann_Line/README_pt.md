# Estratégia de Gann Line
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica as ideias centrais do consultor especialista MetaTrader 4 "Gann Line" (ID de origem 24877) usando a API de alto nível do StockSharp. Mantém os mesmos filtros de tendência, Momentum e MACD de longo prazo enquanto expressa todas as ferramentas de gestão de dinheiro em **passos de preço**, tornando a lógica independente do broker.

## Lógica de trading

1. **Filtro de tendência (período principal)**
   - Duas médias móvias ponderadas linearmente (LWMA) são aplicadas ao preço típico do candle (high + low + close) / 3.
   - Um viés longo requer que a LWMA rápida feche acima da LWMA lenta; um viés curto requer o oposto.
2. **Confirmação de Momentum (período superior)**
   - Um oscilador Momentum calculado em um período superior configurável verifica o quanto o oscilador se desvia do nível de equilíbrio (100).
   - Pelo menos um dos últimos três valores de Momentum terminados deve exceder o limiar de desvio configurado antes que qualquer trade seja permitido.
3. **Filtro MACD lento (período muito alto)**
   - Um filtro MACD calculado em um período lento (mensal por padrão) deve confirmar a direção: linha principal MACD acima do sinal para longos, abaixo para curtos.
4. **Gestão de posições**
   - Alvos fixos de stop-loss e take-profit são convertidos de passos de preço para preços absolutos quando um trade é aberto.
   - A lógica de ponto de equilíbrio opcional move o stop para o preço de entrada mais um offset assim que o trade avançou uma determinada quantidade de passos em lucro.
   - A lógica de trailing opcional desloca o stop atrás do máximo mais alto (para longos) ou mínimo mais baixo (para curtos) assim que o preço percorreu um número configurável de passos.

## Gestão de risco

- Todas as distâncias (stop-loss, take-profit, ponto de equilíbrio e trailing) são inseridas em **passos** de preço. O helper as converte em preços usando o `PriceStep` do instrumento.
- A estratégia trabalha com a propriedade base `Volume`. Se for zero, um contrato/lote é usado por padrão.
- Apenas uma única posição líquida é gerenciada. Sinais opostos fecham o trade atual antes de abrir um novo.

## Diferenças da versão MQL4

- O consultor especialista original dependia de uma linha de tendência Gann desenhada manualmente. StockSharp não expõe objetos do gráfico, então o porte substitui essa verificação pela confirmação de inclinação da LWMA.
- O trailing baseado em dinheiro, fechamentos parciais e verificações de capital de toda a conta do script são simplificados em cálculos determinísticos baseados em passos.
- Notificações (alertas, e-mails, pushes móveis) não são geradas porque as estratégias StockSharp tipicamente registram na saída da plataforma.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `Fast LWMA` | Comprimento da LWMA rápida para o filtro de tendência. |
| `Slow LWMA` | Comprimento da LWMA lenta para o filtro de tendência. |
| `Momentum Period` | Retrospectiva do oscilador Momentum no período secundário. |
| `Momentum Threshold` | Desvio mínimo de 100 necessário por qualquer um dos últimos três valores de Momentum. |
| `MACD Fast / Slow / Signal` | Comprimentos EMA do filtro MACD lento. |
| `Take Profit (steps)` | Distância de take-profit em passos de preço. |
| `Stop Loss (steps)` | Distância de stop-loss em passos de preço. |
| `Use Trailing`, `Trail Activation`, `Trail Distance` | Habilitar trailing, lucro necessário antes de o trailing começar, e distância entre extremo de preço e trailing stop. |
| `Use BreakEven`, `BreakEven Activation`, `BreakEven Offset` | Habilitar ponto de equilíbrio, lucro necessário antes de mover o stop, e lucro adicional garantido depois. |
| `Primary Timeframe` | Tipo de candle usado pelo cruzamento LWMA. |
| `Momentum Timeframe` | Tipo de candle enviado para o oscilador Momentum. |
| `MACD Timeframe` | Tipo de candle enviado para o filtro MACD lento. |

## Dicas de uso

1. Selecione um instrumento e defina o `Primary Timeframe` desejado. Os outros períodos têm padrão de 1 hora (Momentum) e 30 dias (MACD), mas podem ser personalizados.
2. Configure `Volume` e os parâmetros de risco baseados em passos para corresponder às especificações do contrato do seu broker.
3. Execute a estratégia no `Designer` ou através de código. Monitore o log para verificar que filtros, movimentos de ponto de equilíbrio e ajustes de trailing apareçam conforme esperado.
4. Otimize os limiares de Momentum e MACD para adaptar a lógica portada a diferentes mercados ou períodos.

## Melhorias adicionais

- Integrar um stop global baseado em capital semelhante ao script original.
- Substituir o filtro de inclinação LWMA por uma linha de tendência personalizada desenhada no gráfico quando o StockSharp expuser eventos de objetos.
- Adicionar tomada de lucro parcial para imitar o comportamento de múltiplos alvos da versão MQL4.
