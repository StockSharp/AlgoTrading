# Estratégia de Linhas de Reunião AML CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o MetaTrader 5 especialista "Expert_AML_CCI" dentro da estrutura de alto nível StockSharp. O robô original
combina o padrão de vela japonês "Meeting Lines" com um filtro Commodity Channel Index (CCI) e usa o Expert Advisor
motor para ponderar votos de alta e baixa. A porta StockSharp mantém a mesma lógica de confirmação, traduz o castiçal
detecção de padrões em pura aritmética de velas e expõe todos os limites como parâmetros amigáveis ao otimizador.

## Como funciona
* **Fonte de dados** – A estratégia assina uma série de velas de período configurável (velas de 30 minutos por padrão) usando
`SubscribeCandles`. Cada vela finalizada é despachada junto com o valor CCI sincronizado por meio do `Bind` de alto nível
pipeline, portanto, nenhum gerenciamento manual de indicadores é necessário.
* **Indicador principal** – Um único `CommodityChannelIndex` com período `CciPeriod` espelha o oscilador MetaTrader. Seus valores são
armazenado em cache internamente para comparar as duas leituras concluídas mais recentes, replicando as chamadas `CCI(1)` e `CCI(2)` de MQL.
* **Lógica de velas** – Os métodos auxiliares reconstroem as verificações de "Linhas de reunião de alta" e "Linhas de reunião de baixa". Eles calculam
a média móvel dos comprimentos do corpo sobre `AverageBodyPeriod` velas (padrão 3) e aplicar o corpo longo e o fechamento igual
requisitos do filtro `CML_CCI` original. Como StockSharp entrega velas concluídas, o padrão é avaliado com exatidão
quando a segunda vela do padrão fecha – no mesmo momento em que o especialista MQL dá seu voto de 80 pontos.
* **Regras de entrada** –
  * As posições longas exigem uma formação de linhas de reunião de alta e o último valor concluído de CCI permanece abaixo ou igual a
`LongEntryCciLevel` (−50 por padrão). Se uma posição curta oposta estiver aberta, o tamanho da ordem inclui automaticamente o valor absoluto
da posição atual para mudar de direção, correspondendo ao comportamento EA.
  * As posições curtas refletem a lógica: um padrão de linhas de encontro de baixa mais um valor CCI acima ou igual a `ShortEntryCciLevel`
(+50 por padrão).
* **Regras de saída** – Em vez dos pesos de voto do Expert Advisor, a porta usa ordens de achatamento explícitas. As vagas estão fechadas
quando o CCI cruza a banda extrema definida por `ExtremeCciLevel` (80 por padrão):
  * Os shorts saem quando o CCI salta para cima em −Extreme ou cai abaixo de +Extreme.
  * As compras saem quando CCI cai abaixo de +Extremo ou mergulha em −Extremo.
Essas regras refletem a ramificação de voto `40` dentro de `LongCondition` e `ShortCondition` na classe de sinal MQL.
* **Gerenciamento de riscos** – A estratégia deixa paradas de proteção para quem chama. É compatível com StockSharp do `StartProtection`
ajudante se um stop-loss ou take-profit precisar ser anexado externamente.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Período das velas de origem. | Período de 30 minutos |
| `CciPeriod` | Comprimento do Índice de Canal de Commodities. | 18 |
| `AverageBodyPeriod` | Número de velas usadas para calcular o tamanho médio do corpo para validação do padrão. | 3 |
| `LongEntryCciLevel` | Nível de sobrevenda que confirma linhas de reunião de alta. | −50 |
| `ShortEntryCciLevel` | Nível de sobrecompra que confirma linhas de encontro de baixa. | +50 |
| `ExtremeCciLevel` | Banda extrema absoluta para cruzamentos de saída CCI. | 80 |

Todos os parâmetros numéricos expõem intervalos de otimizador idênticos aos padrões de EA para que a estratégia possa ser ajustada por meio de StockSharp
ferramentas de otimização.

## Notas de uso
1. Anexe a estratégia a um título e defina o `Volume` desejado antes de começar.
2. Opcionalmente, ajuste os limites para corresponder ao perfil original de gerenciamento de dinheiro ou para ajustar a sensibilidade.
3. A integração do gráfico desenha velas, a curva CCI e executa negociações para validação visual rápida da detecção de padrão.

Ao focar na mesma combinação de vela + CCI, esta implementação StockSharp oferece uma versão fiel do Expert
Consultor enquanto permanece dentro do estilo API de alto nível recomendado.
