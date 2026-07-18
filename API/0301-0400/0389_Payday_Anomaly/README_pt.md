# Estratégia de Anomalia do Dia de Pagamento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia explora o efeito "payday" mantendo um ETF de mercado amplo em torno das datas típicas de pagamento de salários. O ETF é mantido desde dois dias de negociação antes do fim do mês até o terceiro dia de negociação do novo mês, capturando entradas de capital de contribuições salariais.

O restante do mês a carteira fica em caixa. Velas diárias determinam a janela e ordens de mercado ajustam a posição.

## Detalhes

- **Instrumento**: ETF de mercado amplo.
- **Janela**: de dois dias antes do fim do mês até o terceiro dia de negociação do mês seguinte.
- **Posicionamento**: comprado durante a janela, sem posição caso contrário.
- **Dados**: velas diárias.
- **Controle de risco**: negociação ignorada se o valor da ordem estiver abaixo de `MinTradeUsd`.
