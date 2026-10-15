# Estratégia da Semana de Vencimento de Opções
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia em Python compra e mantém um ETF de ações apenas durante a semana de vencimento de opções. A partir da segunda-feira anterior à terceira sexta-feira de cada mês, o ETF é comprado e a posição é fechada no fechamento da sexta-feira. A ideia explora a força de curto prazo frequentemente observada durante a semana de vencimento.

Fora dessa janela, a carteira permanece em caixa. Velas diárias são usadas e as negociações são enviadas como ordens de mercado uma vez por dia.

## Detalhes

- **Instrumento**: um único ETF de ações.
- **Sinal**: regra de calendário para a semana que termina na terceira sexta-feira.
- **Período de manutenção**: abertura de segunda-feira ao fechamento de sexta-feira da semana de vencimento.
- **Posicionamento**: totalmente investido durante a janela, sem posição caso contrário.
- **Controle de risco**: negociação ignorada quando o valor da ordem estiver abaixo de `MinTradeUsd`.
