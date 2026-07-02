# Estratégia de regressão multiperíodo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia multiperíodo que combina canais de regressão linear em velas M1, M5 e H1. A inclinação de regressão do canal H1 define a tendência dominante, enquanto os canais M5 e M1 fornecem locais de entrada precisos perto do suporte e da resistência.

## Lógica de negociação

- **Feeds de dados**: nove timeframes de velas padrão (M1, M5, M15, M30, H1, H4, D1, W1, MN1).
- **Indicadores**: cada feed é processado por um canal de regressão linear de comprimento configurável. O canal fornece uma linha central e bandas superiores/inferiores simétricas com base no desvio máximo dos fechamentos recentes.
- **Filtro de tendência**: a estratégia considera apenas negociações curtas quando a inclinação do canal H1 for negativa e negociações longas quando for positiva.
- **Entrada**:
  - **Curto** – o máximo M5 e o máximo M1 mais recentes perfuram suas bandas superiores do canal enquanto a inclinação H1 é negativa.
  - **Longo** – o mínimo M5 e o mínimo M1 mais recentes atingem suas bandas de canal mais baixas enquanto a inclinação H1 é positiva.
- **Tratamento de ordens**: os lançamentos são executados com ordens de mercado utilizando o volume configurado. As metas de stop-loss e take-profit são derivadas da meia largura e da linha central do canal M5, respectivamente.
- **Saída**: as posições são fechadas nas velas M1 quando o preço atinge o stop de proteção ou o alvo da linha central.
- **Gerenciamento de posições**: no máximo uma posição de mercado está aberta a qualquer momento.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `EnableTrading` | Permite que a estratégia faça pedidos quando habilitada. |
| `BarsToCount` | Número de barras usadas em cada canal de regressão (padrão 50). |
| `Volume` | Volume de ordens de mercado em lotes. |

## Notas

- Janelas de regressão mais longas proporcionam inclinações de canal mais suaves, mas reações mais lentas.
- A exibição da inclinação de vários períodos de tempo é útil para monitorar o alinhamento em intervalos mais altos, mesmo que apenas a inclinação H1 bloqueie as entradas.
- Os níveis de proteção são recalculados cada vez que uma nova vela M5 se forma; a recalibração frequente mantém o risco fortemente acoplado à geometria atual do canal.
