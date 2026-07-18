# Estratégia JMaster RSX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia JMaster RSX é uma conversão direta do consultor especialista MetaTrader 4 **jMasterRSXv1**. O sistema alinha os valores do oscilador Jurik RSX calculados em um período de tempo rápido (M5) e lento (M30). Quando o período de tempo mais elevado aponta numa direção de alta ou baixa e o oscilador rápido atinge o território de sobrevenda/sobrecompra, a estratégia entra numa posição na direção correspondente. Todos os sinais são avaliados na abertura da nova barra usando as velas totalmente fechadas anteriores, correspondendo à implementação MT4 que referenciou valores `shift = 1`.

## Indicadores e Dados
- **Jurik RSX (Length = `RsxLength`) no timeframe rápido** – avalia o oscilador na série de velas definida por `FastCandleType` (barras padrão de 5 minutos). A conversão reproduz o filtro recursivo original usado pelo indicador `rsx.mq4` personalizado.
- **Jurik RSX no período lento** – calculado com a mesma duração na série de velas definida por `SlowCandleType` (barras padrão de 30 minutos). O último valor lento concluído é atrasado em uma barra antes de ser usado, refletindo o comportamento de mudança do MT4.

## Lógica de entrada
1. Aguarde a abertura de uma nova vela rápida (equivalente ao processamento de uma vela finalizada em StockSharp).
2. Recupere a leitura rápida anterior do RSX e a leitura lenta anterior do RSX (uma vela lenta atrás do fechamento atual).
3. **Configuração longa:** o RSX lento está acima do `MidlineLevel` (padrão 50) *e* o RSX rápido está abaixo do `OversoldLevel` (padrão 25).
4. **Configuração curta:** o RSX lento está abaixo do `MidlineLevel` *e* o RSX rápido está acima do `OverboughtLevel` (padrão 75).
5. Abra uma ordem de mercado com volume `Volume` quando nenhuma posição estiver ativa no momento.

## Sair da lógica
- Fechar uma posição longa aberta assim que as condições curtas forem atendidas (RSX lento abaixo da linha média e RSX rápido acima do limite de sobrecompra).
- Fechar uma posição curta aberta assim que as condições longas forem atendidas (RSX lento acima da linha média e RSX rápido abaixo do limite de sobrevenda).
- A estratégia não acumula posições; sempre se reduz a um estado plano antes de considerar uma nova entrada.

## Dimensionamento de posições
- Os pedidos são feitos com um volume fixo controlado pelo parâmetro `Volume` (padrão `0.1`).
- Nenhuma lógica adaptativa de gestão de dinheiro ou de pirâmide é implementada. Isso reflete o comportamento padrão do EA original quando `DecreaseFactor` foi deixado em zero.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `FastCandleType` | Tipo de vela para cálculo rápido de RSX | `M5` |
| `SlowCandleType` | Tipo de vela para cálculo lento de RSX | `M30` |
| `RsxLength` | Comprimento de lookback compartilhado por ambas as instâncias RSX | `14` |
| `OverboughtLevel` | Limite RSX rápido para entradas curtas | `75` |
| `OversoldLevel` | Limite RSX rápido para entradas longas | `25` |
| `MidlineLevel` | Linha média RSX lenta separando regimes de alta/baixa | `50` |
| `Volume` | Volume de pedidos para entradas no mercado | `0.1` |

## Notas de uso
- Garanta que os dados históricos forneçam velas concluídas para ambos os prazos configurados; a estratégia só reage após o fechamento de uma vela.
- Como o valor RSX lento é deliberadamente atrasado em uma barra, as reversões intrabarras no período de tempo mais alto aparecerão uma barra depois - isso corresponde à fonte EA e evita o viés antecipado.
- O indicador RSX integrado gera valores na faixa de 0 a 100, permitindo comparação direta com outros osciladores, se desejado.
