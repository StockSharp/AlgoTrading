# Estratégia da Pirâmide do Caos FX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

FX Chaos Pyramid é uma estratégia de fuga de vários estágios convertida do consultor especialista MetaTrader 4 "FX-CHAOS" localizado em `MQL/8055`. A porta mantém a lógica multiperíodo original: a execução primária ocorre no período de 4 horas, enquanto o período diário fornece filtros de breakout de nível superior. As inscrições são confirmadas com o filtro de impulso Awesome Oscillator antes que o primeiro estágio seja aberto. Os estágios adicionais formam uma pirâmide na posição existente sempre que a tendência continua no período primário.

A implementação StockSharp usa o API de alto nível com assinaturas de velas, vinculação de indicadores e auxiliares de ordem nativos, de modo que a estratégia pode ser usada tanto para backtesting quanto para negociação ao vivo sem código de infraestrutura extra.

## Lógica de negociação

### Filtro de prazo mais alto

* Assine velas diárias e calcule a última oscilação confirmada do ZigZag usando um detector fractal de 5 velas.
* Armazene os máximos e mínimos do dia anterior. Um buffer configurável em etapas de preço é adicionado a ambos os níveis antes da execução das verificações de rompimento.

### Execução do prazo primário

* Assine velas de 4 horas e vincule o Awesome Oscillator (configuração padrão 5/34).
* Acompanhe a última oscilação fractal no período de 4 horas como um análogo do indicador personalizado `zzf` original.
* Registre a primeira vela aberta de 4 horas para cada novo dia de negociação. Este valor desempenha a mesma função que `iOpen(NULL, 1440, 0)` em MQL.

### Regras de entrada

* **Estágio longo inicial**: o dia atual abre abaixo da máxima diária anterior protegida, o fechamento de 4 horas quebra acima desse nível protegido, o preço ainda permanece abaixo do último fractal ascendente diário e o Oscilador Awesome é negativo. As posições curtas existentes são fechadas antes da abertura das posições longas.
* **Estágio curto inicial**: lógica de espelho com a mínima diária e o Awesome Oscillator acima de zero.

### Estágios da pirâmide

Após o preenchimento do estágio inicial, a estratégia avalia cada vela concluída de 4 horas:

* Uma adição longa é colocada quando a vela abre abaixo e fecha acima da máxima anterior de 4 horas, enquanto o fechamento permanece abaixo do último fractal ascendente do período primário.
* Uma pequena adição usa o mínimo de 4 horas e o último fractal descendente.
* Filtro de patrimônio opcional: etapas posteriores só são permitidas quando o patrimônio do portfólio for maior que o saldo, replicando o requisito `AccountEquity() > AccountBalance()` do especialista MQL.

O número de estágios extras é configurável (até cinco para corresponder à matriz do lote original). Os estágios são redefinidos sempre que a posição é fechada ou quando um sinal de reversão fecha o lado oposto.

## Gestão de capital

O especialista original ajusta a matriz do lote dependendo do saldo da conta. Esta porta mantém as mesmas definições por partes e expõe o equilíbrio base, a etapa de equilíbrio e o multiplicador de volume global como parâmetros. O patrimônio atual do portfólio é mapeado para um intervalo `MAX_Lots` (variando de 3,0 a 15,0 lotes) e o vetor de lote apropriado é selecionado:

| intervalo `MAX_Lots` | Estágio 1 | Estágio 2 | Etapa 3 | Estágio 4 | Estágio 5 |
|------------------|---------|---------|---------|---------|---------|
| <2             | 0,10    | 0,50    | 0,40    | 0h30    | 0,20    |
| [2, 4)           | 0,20    | 1,00    | 0,80    | 0,60    | 0,40    |
| [4, 5)           | 0h30    | 1,50    | 1,20    | 0,90    | 0,60    |
| [5, 7)           | 0,40    | 2h00    | 1,60    | 1,20    | 0,80    |
| [7, 8)           | 0,50    | 2,50    | 2h00    | 1,50    | 1,00    |
| [8, 10)          | 0,60    | 3h00    | 2h40    | 1,80    | 1,20    |
| [10, 11)         | 0,70    | 3,50    | 2,80    | 2.10    | 1,40    |
| [11, 13)         | 0,80    | 4h00    | 3.20    | 2h40    | 1,60    |
| [13, 14)         | 0,90    | 4,50    | 3,60    | 2,70    | 1,80    |
| ≥ 14             | 1,00    | 5h00    | 4h00    | 3h00    | 2h00    |

A multiplicação pelo parâmetro `VolumeScale` permite que a mesma distribuição relativa seja aplicada a diferentes corretoras ou classes de ativos.

## Parâmetros

| Nome | Descrição |
|------|-------------|
| **Vela Primária** | Prazo de negociação usado para entradas (padrão 4 horas). |
| **Vela Diária** | Velas de prazo mais alto que fornecem níveis máximo/mínimo anteriores (padrão 1 dia). |
| **AO Rápido / AO Lento** | Períodos curtos e longos do Awesome Oscillator. |
| **Buffer de interrupção** | Buffer nas etapas de preço adicionado às máximas/mínimas anteriores. |
| **Estágios máximos** | Número máximo de entradas da pirâmide (1-5). |
| **Exigir lucro** | Permita etapas adicionais apenas quando o patrimônio exceder o equilíbrio. |
| **Escala de volume** | Multiplicador global aplicado ao vetor de lote selecionado. |
| **Saldo Básico** | Saldo atribuído ao menor vetor de lote. |
| **Etapa de equilíbrio** | Incremento de equilíbrio que passa para o próximo vetor. |

## Diferenças do especialista MQL4

* A versão StockSharp usa assinaturas de vela integradas em vez de chamadas diretas `iClose`/`iHigh` e armazena os níveis de preços necessários internamente.
* O indicador personalizado `zzf` original é emulado por meio de um detector fractal leve que confirma oscilações de cinco velas.
* A gestão de stop-loss e take-profit não está incluída; o especialista original modificou as paradas dinamicamente, mas o algoritmo dependia fortemente de funções específicas do corretor. Os traders podem adicionar o seu próprio módulo de risco, se necessário.
* Notificações sonoras e variáveis globais de terminal são omitidas intencionalmente.

## Dicas de uso

1. Anexe a estratégia a um portfólio que reporte saldo e patrimônio líquido para que a matriz do lote se comporte exatamente como em MetaTrader.
2. Use dados históricos realistas de 4 horas e diários ao fazer backtesting. Resoluções mistas degradarão a lógica da pirâmide.
3. Experimente o parâmetro `BreakoutBuffer` ao mudar para mercados que usam diferentes tamanhos de ticks ou spreads.
4. Habilite o gráfico durante a depuração: a estratégia traça automaticamente velas, o histograma do Awesome Oscillator e as negociações executadas.
