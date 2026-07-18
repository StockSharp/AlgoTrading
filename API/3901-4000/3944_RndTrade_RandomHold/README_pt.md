# Estratégia RndTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Conversão do consultor especialista MQL4 "RndTrade" original em uma estratégia de alto nível StockSharp que realiza entradas de mercado totalmente aleatórias e sai delas após um período de retenção fixo.

## Lógica principal

1. Assine o tipo de vela configurado (velas de 1 minuto por padrão) e aguarde a conclusão das barras.
2. Sempre que a estratégia for plana, gere um número aleatório. Um valor acima de 0,5 aciona uma compra no mercado, caso contrário, uma venda no mercado, ambos usando o volume de negociação configurado.
3. Registre o tempo da vela da entrada e mantenha a posição aberta pelo período de retenção selecionado (quatro horas por padrão).
4. Depois de decorrido o tempo de espera, feche toda a posição com a ordem de mercado correspondente.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Tipo de dados de velas que disparam a lógica de decisão aleatória. | Velas de 1 minuto |
| `TradeVolume` | Volume usado para cada ordem de mercado aleatória. | 1 |
| `HoldDuration` | Intervalo de tempo para manter ativa qualquer posição aleatória aberta antes de fechá-la. | 4 horas |

## Notas adicionais

- O gerador aleatório é propagado novamente automaticamente quando a estratégia começa a imitar o comportamento MQL4 de usar a hora local como semente.
- Apenas são utilizadas ordens de mercado, refletindo o consultor especialista original que executou imediatamente as negociações sem ordens pendentes.
- Não são necessários indicadores adicionais ou reservas históricas; a estratégia depende apenas dos carimbos de data e hora das velas recebidas e do cronômetro interno.
