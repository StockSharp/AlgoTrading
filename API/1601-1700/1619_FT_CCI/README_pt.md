# Estratégia FT CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port para StockSharp do consultor especialista do MetaTrader 5 "FT_CCI (barabashkakvn's edition)". Usa o Commodity Channel Index (CCI) para capturar reversões bruscas quando o oscilador se afasta muito de sua média. O sistema espelha a lógica original: quando o CCI perfura a banda inferior, ele muda para comprado, e quando perfura a banda superior, muda para vendido. Os valores opcionais de stop-loss e take-profit são inseridos em pips e convertidos automaticamente em deslocamentos de preço.

## Visão Geral
- **Indicador principal**: Commodity Channel Index com um período de suavização configurável (padrão 14).
- **Viés**: Comprado/Vendido simétrico. A estratégia mantém no máximo uma posição líquida e reverte em sinais opostos.
- **Execução**: Ordens de mercado no fechamento das velas concluídas do período selecionado.
- **Gestão de risco**: Distâncias opcionais de stop-loss e take-profit expressas em pips. Se qualquer valor for zero, a proteção correspondente é desativada.
- **Período padrão**: Velas de 30 minutos (espelha a seleção de `Period()` no especialista original).

## Como funciona
### Configuração comprada
1. Subscrever às velas concluídas do período selecionado.
2. Atualizar o indicador CCI com valores de preço típico.
3. Quando o último valor do CCI estiver no ou abaixo do limiar inferior configurado (padrão -210):
   - Fechar qualquer exposição vendida aberta.
   - Entrar ou adicionar a uma posição comprada usando o volume de negociação configurado.
4. Manter a posição até que uma configuração vendida oposta acione, ocorra um evento de stop-loss/take-profit ou a estratégia seja parada manualmente.

### Configuração vendida
1. Monitorar os mesmos valores de CCI em velas concluídas.
2. Quando o indicador estiver no ou acima do limiar superior (padrão +210):
   - Fechar qualquer exposição comprada aberta.
   - Entrar ou adicionar a uma posição vendida usando o volume configurado.
3. Manter a posição vendida até que uma condição comprada oposta acione ou ordens de proteção fechem a negociação.

### Gestão de negociações
- As distâncias de stop-loss e take-profit são definidas em pips. A estratégia os multiplica pelo tamanho de pip detectado (passo de preço, multiplicado por 10 para símbolos forex de 3 e 5 dígitos) para obter um deslocamento de preço absoluto antes de ativar o `StartProtection` integrado do StockSharp.
- Como a proteção é aplicada uma vez no início, qualquer nova posição herda imediatamente os mesmos valores de stop e alvo relativos ao seu preço de execução.
- Os giros de posição são executados por ordens de mercado dimensionadas em `volume configurado + |posição atual|`, garantindo que reverter uma posição tanto fecha a exposição atual quanto abre a nova em uma única transação.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| **Candle Type** | Período usado para cálculos e geração de sinal. |
| **Trade Volume** | Tamanho do lote para novas posições. Usado junto com o valor da posição atual para dimensionar negociações de reversão. |
| **CCI Period** | Comprimento de suavização do Commodity Channel Index. |
| **CCI Upper Threshold** | Nível de CCI que aciona entradas vendidas. |
| **CCI Lower Threshold** | Nível de CCI que aciona entradas compradas. |
| **Stop Loss (pips)** | Distância ao stop de proteção em pips. Definir como 0 para desativar. |
| **Take Profit (pips)** | Distância ao alvo de lucro em pips. Definir como 0 para desativar. |

Todos os parâmetros suportam otimização através do gerenciador de parâmetros do StockSharp.

## Uso recomendado
- Funciona melhor em pares forex líquidos e índices onde velas de 30 minutos a 4 horas produzem extremos de CCI pronunciados.
- Os limiares de ±210 recriam os padrões do FT_CCI. Valores mais baixos tornam o sistema mais reativo; valores mais altos focam apenas nas reversões mais extremas.
- Certifique-se de que os metadados do instrumento expõem um `PriceStep` válido. O conversor de pips depende desse valor para traduzir pips em deslocamentos de preço.
- A estratégia assume um modelo de conta de compensação (posição líquida única). Para contas de cobertura, defina o volume de negociação adequadamente para que as reversões aplainassem completamente a negociação anterior.

## Notas
- O indicador deve estar completamente formado antes que qualquer sinal de negociação seja considerado. As primeiras velas são ignoradas até que o CCI tenha dados suficientes para emitir valores válidos.
- As ordens de stop-loss e take-profit são opcionais. Deixá-las em zero reproduz o comportamento original do consultor especialista que dependia exclusivamente de sinais opostos para saídas.
- Adicione a estratégia a um gráfico no StockSharp para visualizar velas, o indicador CCI e negociações executadas; essas ajudas visuais são habilitadas automaticamente na implementação em C#.
