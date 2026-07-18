# Estratégia MARE5.1 com Média Móvel Deslocada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A **Estratégia MARE5.1 com Média Móvel Deslocada** é um port direto do assessor especialista original do MetaTrader 5 "MARE5.1" para a API de alto nível do StockSharp. O sistema monitora velas de um minuto (configuráveis) e compara duas médias móveis simples (SMA) que compartilham um deslocamento configurável para frente. A lógica busca padrões de cruzamento confirmados por relações históricas de SMA e a direção da última vela completada.

## Lógica de Trading

- A estratégia usa duas SMAs: uma SMA rápida e uma SMA lenta. Ambas são deslocadas para frente pelo mesmo número de barras, replicando o comportamento do assessor especialista original.
- Uma **posição vendida** é aberta quando tudo o seguinte é verdadeiro:
  1. A SMA lenta está pelo menos um passo de preço acima da SMA rápida na vela atual.
  2. Duas velas atrás, a SMA rápida estava pelo menos um passo de preço acima da SMA lenta.
  3. Cinco velas atrás, a SMA rápida estava pelo menos um passo de preço acima da SMA lenta.
  4. A vela completada mais recente (barra anterior) é de baixa.
- Uma **posição comprada** é aberta quando o padrão oposto ocorre:
  1. A SMA rápida está pelo menos um passo de preço acima da SMA lenta na vela atual.
  2. Duas velas atrás, a SMA lenta estava pelo menos um passo de preço acima da SMA rápida.
  3. Cinco velas atrás, a SMA lenta estava pelo menos um passo de preço acima da SMA rápida.
  4. A vela completada mais recente (barra anterior) é de alta.
- Apenas uma posição pode estar aberta de cada vez. O tamanho padrão da ordem vem do parâmetro `TradeVolume`.
- O trading só é permitido entre as horas de sessão configuradas (inclusive). Esta janela replica o filtro baseado em horas do assessor especialista original.

## Gestão de Risco

A estratégia espelha as distâncias fixas originais de take profit e stop-loss. Elas são definidas em "pips" (pontos ajustados para instrumentos de três e cinco dígitos) e convertidos em unidades de preço absolutas quando a estratégia começa. As ordens de proteção são gerenciadas através de `StartProtection` com saídas de ordens de mercado.

## Indicadores e Dados

- **SMA rápida** – comprimento definido por `FastPeriod`.
- **SMA lenta** – comprimento definido por `SlowPeriod`.
- **Fonte de dados** – por padrão velas de um minuto, mas qualquer tipo de vela suportado pelo StockSharp pode ser selecionado através do parâmetro `CandleType`.

## Parâmetros

| Nome | Valor padrão | Descrição |
|------|--------------|-----------|
| `TradeVolume` | 0.01 | Volume da ordem usado para entradas. |
| `TakeProfitPips` | 35 | Distância do take profit em pips ajustados. Definir como zero para desabilitar. |
| `StopLossPips` | 55 | Distância do stop-loss em pips ajustados. Definir como zero para desabilitar. |
| `FastPeriod` | 14 | Período da SMA rápida. |
| `SlowPeriod` | 79 | Período da SMA lenta. |
| `MovingAverageShift` | 4 | Deslocamento para frente (em barras) aplicado a ambas as SMAs. |
| `SessionOpenHour` | 2 | Início da janela de trading permitida (0–23, inclusive). |
| `SessionCloseHour` | 3 | Fim da janela de trading permitida (0–23, inclusive). Deve ser maior que `SessionOpenHour`. |
| `CandleType` | Velas de 1 minuto | Tipo de dados de velas usado pela estratégia. |

## Notas

- Os sinais são avaliados em velas completadas. Valores históricos de SMA são armazenados internamente para replicar as comparações baseadas em índice do código MQL original.
- O valor do passo de preço do instrumento ativo é usado ao comparar diferenças de SMA para garantir que a distância necessária seja pelo menos um tick.
- Os níveis de stop-loss e take profit dependem do passo de preço do instrumento. Para instrumentos de três e cinco decimais, o tamanho do pip é automaticamente expandido dez vezes, correspondendo ao comportamento do MetaTrader.
- Nenhum dimensionamento automático de posição é implementado. A estratégia aguarda o fechamento de todas as posições abertas antes de procurar o próximo sinal de entrada.
- Este repositório contém apenas a implementação em C#; não há port em Python para esta estratégia.
