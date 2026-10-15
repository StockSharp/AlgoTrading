# Estratégia de Reversão do Histograma RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia opera contra valores extremos do RVI. Opera com o Índice de Vigor Relativo (RVI) e abre posições quando o indicador abandona zonas de sobrecompra ou sobrevenda, ou quando o RVI cruza a sua linha de sinal. Dois modos de sinal são suportados:

- **Levels** – reage quando o RVI cruza limiares superiores ou inferiores predefinidos.
- **Cross** – reage quando o RVI cruza a sua linha de sinal.

A lógica é contrária: se o RVI estava acima do nível alto e depois desce, abre-se uma posição comprada. Se o RVI estava abaixo do nível baixo e depois sobe, abre-se uma posição vendida.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `RviPeriod` | Período de cálculo do RVI. |
| `HighLevel` | Limiar superior do RVI. |
| `LowLevel` | Limiar inferior do RVI. |
| `Mode` | Modo de geração de sinal (`Levels` ou `Cross`). |
| `EnableBuyOpen` | Permitir abertura de posições compradas. |
| `EnableSellOpen` | Permitir abertura de posições vendidas. |
| `EnableBuyClose` | Permitir fecho de posições compradas. |
| `EnableSellClose` | Permitir fecho de posições vendidas. |
| `CandleType` | Período do ローソク足. |

## Como funciona

1. O RVI e a sua média móvel simples são calculados em cada vela concluída.
2. Dependendo do modo selecionado, a estratégia verifica:
   - se o RVI abandona um nível extremo, ou
   - se o RVI cruza a sua linha de sinal.
3. Com um sinal comprado, a estratégia fecha posições vendidas e abre uma posição comprada. Com um sinal vendido, fecha posições compradas e abre uma posição vendida.

O período padrão é de quatro horas.

## Notas

- As ordens são executadas com ordens de mercado.
- A gestão de stop-loss e take-profit pode ser adicionada separadamente se necessário.
