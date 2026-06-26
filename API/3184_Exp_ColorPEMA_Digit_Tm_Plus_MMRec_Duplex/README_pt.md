# Exp Color PEMA Digit TM Plus MMRec Duplex (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia recria o Expert Advisor "Exp_ColorPEMA_Digit_Tm_Plus_MMRec_Duplex" usando a API de alto nível do StockSharp. Ela opera com dois fluxos independentes de Média Móvel Exponencial Quíntupla (PEMA) que podem usar diferentes períodos e fontes de preço. O módulo longo abre operações quando a inclinação do PEMA muda para alta, enquanto o módulo curto reage a viradas de baixa. Cada lado suporta saídas baseadas em indicadores e um temporizador de segurança que força o fechamento da posição após um número configurável de minutos.

## Indicadores
* **PEMA quíntupla** – indicador personalizado que encadeia oito médias exponenciais de mesmo comprimento e as combina usando os coeficientes clássicos (8, -28, 56, -70, 56, -28, 8, -1). O indicador expõe tanto o valor atual quanto a amostra anterior para que a estratégia possa classificar a direção da inclinação (para cima, para baixo, plana).
* **Lógica de cores** – a inclinação é mapeada para três estados discretos: para cima (verde), para baixo (magenta) e neutro (cinza), reproduzindo o comportamento do indicador ColorPEMA original.

## Geração de sinais
### Módulo longo
1. Aguarda uma vela concluída no período longo selecionado.
2. Solicita o valor do PEMA usando o modo de preço configurado e os dígitos de arredondamento.
3. Avalia o estado de cor `SignalBar` velas atrás e o compara com a barra anterior.
4. **Entrada**: quando a cor muda para `Up` e as entradas são permitidas, a estratégia compra usando o `TradeVolume` compartilhado e armazena o horário de entrada.
5. **Saída**: quando a cor muda para `Down`, a estratégia fecha a posição longa se as saídas baseadas em indicadores estiverem habilitadas.
6. **Guarda temporal**: se a posição longa aberta sobreviver mais de `LongTimeExitMinutes`, ela é fechada independentemente do estado do indicador.

### Módulo curto
O lado curto repete o mesmo fluxo de trabalho de forma independente:
1. Monitorar as velas do período curto.
2. Calcular a série PEMA curta.
3. Buscar `ShortSignalBar` velas atrás para detectar uma mudança para a cor `Down`.
4. **Entrada**: quando a cor se torna baixista e os curtos estão habilitados, a estratégia vende.
5. **Saída**: quando a cor se torna `Up`, o curto é coberto se as saídas forem permitidas.
6. **Guarda temporal**: se `ShortTimeExitMinutes` for excedido, a posição curta é fechada.

## Gestão de risco
* Usa o parâmetro `TradeVolume` para configurar o tamanho padrão da ordem.
* Stop-loss e take-profit opcionais podem ser definidos em passos de preço. Quando qualquer um deles é positivo, a estratégia habilita `StartProtection` com ordens de saída a mercado, espelhando a proteção de gestão monetária presente na versão MQL.
* Temporizadores de saída baseados em tempo independentes para os módulos longo e curto evitam que as operações sejam executadas indefinidamente.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `LongCandleType` | Período usado para o fluxo de indicadores longo. |
| `ShortCandleType` | Período para o fluxo de indicadores curto. |
| `LongEmaLength`, `ShortEmaLength` | Comprimentos de suavização do PEMA quíntuplo (valores fracionários suportados). |
| `LongPriceMode`, `ShortPriceMode` | Modo de preço aplicado para cada fluxo (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado, simples, quarto, seguidor de tendência e Demark). |
| `LongDigits`, `ShortDigits` | Arredondamento decimal aplicado aos valores calculados do PEMA. |
| `LongSignalBar`, `ShortSignalBar` | Número de barras completadas atrás para avaliar a mudança de cor. |
| `LongAllowOpen`, `ShortAllowOpen` | Habilitar/desabilitar novas entradas para cada lado. |
| `LongAllowClose`, `ShortAllowClose` | Habilitar/desabilitar saídas baseadas em indicadores. |
| `LongAllowTimeExit`, `ShortAllowTimeExit` | Ativar ou desativar o temporizador de saída baseado em tempo. |
| `LongTimeExitMinutes`, `ShortTimeExitMinutes` | Tempo máximo de retenção em minutos para operações longas e curtas. |
| `TradeVolume` | Volume padrão para ordens de mercado. |
| `StopLossSteps`, `TakeProfitSteps` | Distâncias protetoras opcionais expressas em passos de preço do instrumento. |

## Notas
* A estratégia assina ambas as séries de velas longas e curtas; se ambos os parâmetros apontarem para o mesmo período, o StockSharp reutiliza automaticamente o feed de dados.
* Ambos os módulos compartilham as mesmas configurações de instrumento e volume, garantindo comportamento simétrico.
* Os cálculos de indicadores são executados apenas em velas concluídas para evitar repintura.
