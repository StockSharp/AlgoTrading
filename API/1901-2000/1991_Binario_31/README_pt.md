# Estratégia Binario 31
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento convertida do script MetaTrader **binario_31**. O algoritmo constrói duas médias móveis exponenciais de 144 períodos calculadas sobre os preços máximos e mínimos dos candles, criando um canal dinâmico. Enquanto o preço permanece dentro do canal, a estratégia prepara ordens de stop de entrada:

- uma compra stop colocada acima da EMA-alta mais um deslocamento configurável;
- uma venda stop colocada abaixo da EMA-baixa menos o mesmo deslocamento.

Quando o preço rompe um desses níveis, uma posição é aberta na direção do rompimento. Um stop de proteção é colocado no lado oposto do canal e uma meta de take profit é calculada em relação à entrada. Um trailing stop opcional pode ser habilitado para proteger os lucros.

## Parâmetros

- **EMA Length** – período para as EMAs de máximos e mínimos.
- **Pip Difference** – distância do nível da EMA até a entrada de rompimento em passos de preço.
- **Take Profit** – distância da entrada até o take profit em passos de preço.
- **Trailing Stop** – distância do trailing stop em passos de preço. Definir como zero para desativar.
- **Volume** – volume da ordem.
- **Candle Type** – tipo de candles que a estratégia assina.
