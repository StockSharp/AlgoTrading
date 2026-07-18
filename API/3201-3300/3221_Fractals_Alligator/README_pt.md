# Estratégia de Fractals & Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o especialista "Fractals & Alligator" do MetaTrader combinando o alinhamento do Alligator de Bill Williams com rompimentos de fractais, uma camada de confirmação de momentum e filtros de intervalo. Ela processa velas finalizadas em um período de tempo superior para emular a lógica multi-temporal original.

## Detalhes
- **Critérios de entrada**: Aguardar que os lábios, dentes e mandíbula do Alligator se alarguem na mesma direção enquanto um fractal fresco se forma além da boca. Uma configuração comprada requer que o fechamento rompa o último fractal altista acima dos dentes e que qualquer uma das últimas três leituras de momentum exceda o limiar de compra. Vendidas espelham as regras na parte inferior.
- **Comprado/Vendido**: Abre operações tanto compradas quanto vendidas. Apenas uma posição líquida é mantida; novos sinais revertem a exposição existente.
- **Critérios de saída**: Posições são fechadas quando o fractal oposto é penetrado ou quando o alinhamento do Alligator colapsa. Ordens protetoras tratam das saídas restantes.
- **Stops**: Usa ordens protetoras do StockSharp para stop-loss, take-profit e um trailing stop opcional em passos de preço, correspondendo à ideia de gestão monetária original.
- **Valores padrão**: Comprimentos do Alligator 13/8/5 com deslocamentos 8/5/3, momentum de 14 períodos, retrocesso de intervalo de 10 barras, caixa fixa de 20 passos (se o filtro ATR estiver desabilitado), take-profit 50 passos, stop-loss 20 passos, trailing stop 40 passos.
- **Filtros**: O multiplicador ATR opcional confirma que o preço se moveu pelo menos um ATR do intervalo recente; caso contrário, uma caixa fixa expressa em passos de preço é usada. Os limiares de momentum (0,3%) suprimem rompimentos de baixa energia.
