# Estratégia Blau Ergodic MDI Time
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A **Estratégia Blau Ergodic MDI Time** é uma conversão direta do especialista MetaTrader `Exp_BlauErgodicMDI_Tm.mq5` para o StockSharp. Opera em velas de período superior e reproduz os três modos de sinal do algoritmo original: **Breakdown**, **Twist** e **CloudTwist**. A estratégia baseia-se num processo de suavização de média móvel exponencial (EMA) de múltiplos estágios aplicado a um preço de vela selecionado. Todos os cálculos são realizados dentro da estratégia sem indicadores adicionais para que a lógica corresponda ao especialista MetaTrader enquanto permanece compatível com a API de alto nível do StockSharp.

O pipeline de suavização segue a lógica do oscilador Blau Ergodic MDI:

1. Suavizar o preço escolhido com uma EMA (comprimento `BaseLength`).
2. Subtrair o valor suavizado do preço bruto para obter uma série de diferenças.
3. Aplicar três EMAs consecutivas à diferença (comprimentos `FirstSmoothingLength`, `SecondSmoothingLength`, `ThirdSmoothingLength`).
4. Escalar as saídas intermediária (`histogram`) e final (`signal`) pelo passo de preço do instrumento. Esses valores impulsionam os sinais de trading.

## Modos de Sinal

### Breakdown

* Usa o histograma duas barras atrás (controlado por `SignalBar`).
* Quando o valor anterior do histograma é positivo e a barra selecionada se move para território não positivo, a estratégia prepara uma entrada comprada e opcionalmente fecha posições vendidas.
* Quando o valor anterior do histograma é negativo e a barra selecionada sobe para território não negativo, a estratégia prepara uma entrada vendida e opcionalmente fecha posições compradas.

### Twist

* Compara a inclinação do histograma ao longo de duas barras históricas.
* Se o histograma acelera para cima (barra `SignalBar + 1` < barra `SignalBar + 2`) e a barra selecionada mais recente está acima da anterior, um sinal de entrada comprada é gerado. Posições vendidas podem ser fechadas no mesmo bloco.
* Se o histograma acelera para baixo (barra `SignalBar + 1` > barra `SignalBar + 2`) e a barra selecionada mais recente está abaixo da anterior, a estratégia prepara uma entrada vendida e pode fechar posições compradas.

### CloudTwist

* Usa tanto o histograma quanto a linha suavizada adicional.
* Quando o histograma anterior permanece acima da linha de sinal mas a barra selecionada cai abaixo, uma entrada comprada é preparada e posições vendidas podem ser fechadas.
* Quando o histograma anterior está abaixo da linha de sinal mas a barra selecionada cruza acima, a estratégia prepara uma entrada vendida e pode sair de posições compradas.

## Filtro de Janela de Tempo

O especialista original restringe o trading a uma sessão configurável. A versão StockSharp replica as mesmas regras através dos parâmetros `UseTimeFilter`, `StartHour`, `StartMinute`, `EndHour` e `EndMinute`. A lógica de sessão suporta janelas que cruzam a meia-noite, idêntica à implementação MetaTrader:

* Se a hora de início é anterior à hora de fim, a sessão permanece dentro de um dia.
* Se a hora de início é igual à hora de fim, os minutos definem um intervalo mais curto durante essa hora.
* Se a hora de início é posterior à hora de fim, a sessão se estende além da meia-noite.

Sempre que o trading é desabilitado pelo filtro de sessão, a estratégia fecha a mercado qualquer posição aberta e bloqueia novas entradas até que a sessão reabra.

## Gestão de Risco

Os parâmetros `StopLossPoints` e `TakeProfitPoints` espelham as distâncias de stop-loss e take-profit do especialista. As distâncias são expressas em passos de preço. A estratégia recalcula os preços de proteção sempre que uma nova posição é aberta. Cada vela finalizada verifica se o intervalo da barra tocou algum nível de proteção e fecha imediatamente a posição se acionado.

## Entradas de Preço

O parâmetro `PriceMode` expõe a mesma lista de fontes de preço que o indicador original:

| Modo | Descrição |
| ---- | --------- |
| Close | Preço de fechamento. |
| Open | Preço de abertura. |
| High | Preço máximo. |
| Low | Preço mínimo. |
| Median | (High + Low) / 2. |
| Typical | (High + Low + Close) / 3. |
| Weighted | (High + Low + 2 × Close) / 4. |
| Simple | (Open + Close) / 2. |
| Quarter | (Open + High + Low + Close) / 4. |
| TrendFollow0 | High em velas de alta, Low em de baixa, Close em neutras. |
| TrendFollow1 | Média de Close com o extremo da vela na direção da tendência. |
| Demark | Preço Demark (ponderado pela direção da vela). |

## Parâmetros

| Parâmetro | Padrão | Descrição |
| --------- | ------ | --------- |
| `Mode` | Twist | Seleciona a avaliação de sinal Breakdown, Twist ou CloudTwist. |
| `PriceMode` | Close | Fonte de preço usada para o oscilador. |
| `BaseLength` | 20 | Comprimento de EMA aplicado ao preço bruto. |
| `FirstSmoothingLength` | 5 | Comprimento de EMA da primeira suavização de diferenças. |
| `SecondSmoothingLength` | 3 | Comprimento de EMA da segunda suavização de diferenças. |
| `ThirdSmoothingLength` | 8 | Comprimento de EMA da terceira suavização de diferenças. |
| `SignalBar` | 1 | Número de barras completadas atrás usadas para verificações de sinal (1 corresponde ao padrão MetaTrader). |
| `AllowLongEntry` / `AllowShortEntry` | true | Habilitar ou desabilitar entradas comprado/vendido. |
| `AllowLongExit` / `AllowShortExit` | true | Habilitar ou desabilitar saídas para o lado correspondente. |
| `UseTimeFilter` | true | Ativa o filtro de sessão de trading. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | 0/0/23/59 | Limites da sessão. |
| `StopLossPoints` | 1000 | Distância de stop-loss em passos de preço (0 desabilita). |
| `TakeProfitPoints` | 2000 | Distância de take-profit em passos de preço (0 desabilita). |
| `CandleType` | Período 4h | Assinatura de velas usada para cálculos. |
| `Volume` | 0.1 | Volume da ordem, correspondendo ao input `MM` do especialista. |

## Resumo das Regras de Trading

1. Assinar velas do período configurado.
2. Em cada vela finalizada, atualizar o pipeline EMA de quatro estágios e armazenar os valores do histograma e do sinal em buffers deslizantes.
3. Aguardar até que a profundidade mínima de histórico seja alcançada (correspondendo ao cálculo original de `min_rates_total`).
4. Avaliar o modo selecionado usando a barra `SignalBar` e valores mais antigos para definir flags de abertura/fechamento.
5. Fechar posições primeiro se o flag de saída correspondente for acionado ou se o filtro de tempo bloquear o trading.
6. Abrir novas operações compradas ou vendidas apenas quando o flag respectivo estiver definido, o filtro de tempo permitir o trading e a posição atual não apontar já na mesma direção. Ao reverter, a estratégia dimensiona automaticamente a ordem para cobrir a exposição existente mais o volume configurado.
7. Manter stops e alvos de proteção usando extremos de velas para detectar acionamentos.

## Notas de Uso

* A estratégia usa tabulações para indentação, consistente com as diretrizes do projeto.
* Chama `StartProtection()` uma vez durante a inicialização para manter os recursos de segurança do StockSharp alinhados com as mudanças de posição.
* Os valores do indicador são armazenados apenas para o número mínimo de barras exigidas pelos sinais. Não são criadas grandes coleções, seguindo as instruções do repositório.
* Para experimentar com outros métodos de suavização da versão MetaTrader, ajustar os comprimentos de EMA adequadamente. O pipeline baseado em EMA fornece a aproximação mais próxima suportada pela API de alto nível do StockSharp.

## Execução da Estratégia

1. Adicionar a classe de estratégia à sua solução StockSharp e compilar o projeto.
2. Configurar os parâmetros (instrumento, período de velas, modo, sessão e configurações de risco).
3. Anexar a estratégia a um conector que forneça os dados de mercado necessários.
4. Iniciar a estratégia; ela assinará automaticamente as velas configuradas e gerenciará ordens de acordo com as regras acima.
