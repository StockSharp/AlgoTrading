# Estratégia Crossover 2 EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o expert advisor "Crossover_2EMA" do MetaTrader negociando a relação entre uma média móvel exponencial (EMA) rápida e uma lenta calculadas a partir dos preços de fechamento. Quando a EMA rápida sobe acima da lenta, o algoritmo fica comprado. Quando ela volta a cair abaixo, o algoritmo reverte para uma posição vendida. A abordagem mantém a posição sempre alinhada ao estado atual da tendência rápida/lenta e, portanto, funciona como um sistema totalmente reversível.

## Lógica de negociação
1. Assinar a série de candles configurada e calcular duas EMAs com períodos definidos pelo usuário.
2. Acompanhar o spread entre os valores da EMA rápida e lenta em cada candle concluído.
3. Detectar um cruzamento para cima quando o spread passa de não positivo para positivo. Fechar qualquer exposição vendida e abrir uma posição comprada com o volume configurado.
4. Detectar um cruzamento para baixo quando o spread passa de não negativo para negativo. Fechar qualquer exposição comprada e abrir uma posição vendida com o volume configurado.
5. As ordens são emitidas a mercado para garantir reação imediata ao cruzamento. O volume é aumentado automaticamente na reversão para zerar a posição existente antes de abrir uma nova.

## Gestão de risco
- A estratégia invoca `StartProtection()` no lançamento para que os mecanismos protetores padrão do StockSharp possam ser configurados (por exemplo, proteção contra drawdown, limites de horário de negociação ou circuit breakers).
- Reversões de posição usam uma única ordem a mercado combinada, reduzindo latência em comparação com saída e reentrada sequenciais.

## Parâmetros
- **Candle Type:** série de dados usada para os cálculos das EMAs.
- **Fast EMA Period:** período da EMA rápida. Deve ser menor que o período da EMA lenta.
- **Slow EMA Period:** período da EMA lenta. Deve ser maior que o período da EMA rápida.

## Notas adicionais
- As duas EMAs devem estar completamente formadas antes do início da negociação, evitando sinais prematuros.
- A configuração padrão usa EMAs de 12/24 períodos em candles de um minuto, espelhando o expert advisor MQL original.
- Os parâmetros são marcados como otimizáveis, permitindo otimização em lote no StockSharp.
