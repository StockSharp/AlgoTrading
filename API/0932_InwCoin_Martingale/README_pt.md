# Estratégia Martingale InwCoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa uma abordagem martingale simples para posições compradas em Bitcoin.
Suporta três sinais de entrada opcionais: histograma MACD cruzando acima de zero,
%D do Stochastic RSI cruzando acima do nível 20, ou o preço rompendo um canal baseado em ATR.
Após cada compra, o tamanho da posição pode dobrar quando o preço cai um percentual configurado.
A posição inteira é fechada quando o lucro atinge um percentual especificado acima do preço médio de entrada.

## Detalhes

- **Sinais de entrada**
  - **MACD Line > 0**: histograma cruza acima de zero.
  - **STO RSI cross up**: linha %D cruza acima de 20 enquanto %K está na zona de sobrevenda.
  - **ATR Channel**: preço de fechamento cruza acima da EMA mais o multiplicador ATR.
- **Take profit**: posição sai quando o preço supera o preço médio pelo percentual configurado.
- **Martingale**: compras adicionais ocorrem quando o preço cai o percentual configurado a partir do preço médio.
- **Direção**: Somente comprado.

