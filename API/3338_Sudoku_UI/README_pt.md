# Estratégia de IU do Sudoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma porta StockSharp do script MetaTrader 5 **SudokuUI.mq5**. O programa MQL original expõe uma interface gráfica de quebra-cabeça Sudoku com parâmetros que controlam a geração, embaralhamento e atualizações automáticas do quebra-cabeça. Como o ambiente StockSharp se concentra na negociação automatizada em vez de widgets de gráficos interativos, a porta reaproveita os conceitos subjacentes em uma estratégia de reversão à média baseada em grade, impulsionada por estatísticas de quebra-cabeças.

O tabuleiro Sudoku é interpretado como uma matriz de dígitos 9x9. As médias das colunas definem limites de desvio simétrico em torno de uma média móvel simples (SMA). Quando o preço se desvia de SMA além desses níveis derivados do Sudoku, a estratégia entra em uma posição na direção oposta, buscando uma reversão de volta à média. Retornar para uma zona neutra fecha a posição, imitando a capacidade da ferramenta original de reiniciar o tabuleiro.

## Lógica de negociação

1. **Preparação de quebra-cabeças**
   - A estratégia pode carregar uma especificação de Sudoku de 81 dígitos de um arquivo ou de uma string bruta. Os caracteres que não sejam dígitos são ignorados e os zeros são ignorados, correspondendo aos requisitos de dígitos do Sudoku.
   - Quando nenhum quebra-cabeça válido é fornecido, um tabuleiro pseudo-aleatório é gerado embaralhando repetidamente os conjuntos de dígitos. A lógica respeita as sementes *embaralhadas* e *composição* que foram expostas na versão MQL para que os traders possam obter layouts reproduzíveis.
   - Um dígito específico pode ser eliminado antes que as estatísticas sejam calculadas. Isso imita a opção original da GUI que ocultava certos rótulos e fornece uma maneira fácil de reduzir a grade ativa.

2. **Construção de nível**
   - A média de cada coluna do quebra-cabeça é calculada após a etapa de eliminação. A média é normalizada para o intervalo [-1, 1] e multiplicada por `ThresholdRange`, produzindo níveis de desvio de preço expressos como frações do valor SMA.
   - Níveis negativos ou positivos de fallback serão inseridos se o quebra-cabeça produzir apenas valores em um lado do SMA, garantindo que existam gatilhos longos e curtos.

3. **Geração de sinal**
   - A estratégia assina o tipo de vela configurado e o vincula a um indicador SMA. Apenas velas finalizadas são processadas, seguindo StockSharp práticas recomendadas.
   - Quando a distância percentual entre o preço de fechamento e o SMA cruza abaixo do nível mais negativo, uma posição longa é aberta (após o achatamento das posições vendidas). Cruzar acima do nível positivo mais alto abre uma posição curta da mesma maneira.
   - Uma banda neutra em torno do desvio zero (`NeutralBand`) força a exposição plana. Isso substitui o "assistente" do Sudoku que ajustava automaticamente o estado do quebra-cabeça.

4. **Atualização automática**
   - Definir `EnableAutoUpdate` como `true` faz com que a grade Sudoku se regenere no início de cada dia de negociação. As sementes de embaralhamento, as configurações de eliminação e a contagem de embaralhamento influenciam os limites recalculados, fornecendo uma grade dinâmica, porém reproduzível.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `PuzzleDefinition` | Caminho do arquivo ou dígitos embutidos que descrevem o quebra-cabeça Sudoku usado para cálculos de nível. |
| `ShufflingRandomSeed` | Semente primária para geração de quebra-cabeças. `-1` deriva a semente do dia de negociação. |
| `CompositionRandomSeed` | Semente secundária que perturba o processo de embaralhamento para criar layouts alternativos. |
| `ShufflingCycles` | Número de passes de embaralhamento adicionais aplicados ao conjunto de dígitos. Valores mais altos criam tabuleiros mais aleatórios. |
| `EliminateLabel` | Dígito (1-9) removido do quadro antes de calcular as médias. `0` mantém todos os dígitos. |
| `EnableAutoUpdate` | Reconstrua os níveis do quebra-cabeça quando a data de negociação mudar. |
| `SmaPeriod` | Comprimento do indicador SMA usado como âncora de reversão. |
| `ThresholdRange` | Desvio absoluto máximo (expresso como uma fração do preço) produzido pelo quebra-cabeça. |
| `NeutralBand` | Zona de desvio que desencadeia o achatamento da posição quando o preço entra novamente nela. |
| `Volume` | Volume de pedidos para entradas no mercado. |
| `CandleType` | Assinatura de velas usada para atualizações de indicadores. |

## Notas de uso

- A estratégia reage apenas a velas totalmente formadas e ignora preços zero, garantindo um comportamento estável entre os provedores de dados.
- Forneça uma sequência de dígitos de 81 caracteres (sem zeros) ou um arquivo de texto contendo esses dígitos para reproduzir exatamente um tabuleiro Sudoku da versão MetaTrader.
- Se você precisar de uma grade estacionária, desative `EnableAutoUpdate` e defina sementes explícitas. A ativação da opção reflete o MQL "assistente automático" que mantinha o quadro sincronizado com as ações do usuário.
- Os limites são derivados das estatísticas da coluna. Para quebra-cabeças assimétricos, considere eliminar o dígito dominante para manter uma cobertura equilibrada de compra/venda.

## Diferenças do roteiro original

- Todos os recursos da interface do usuário (janelas de diálogo, botões, eventos do gráfico) foram removidos. Seus equivalentes funcionais são expostos como parâmetros estratégicos.
- Em vez de resolver quebra-cabeças de Sudoku manualmente, o tabuleiro influencia os níveis de negociação algorítmica. Os mesmos controles de aleatoriedade determinam o quão agressivos ou conservadores esses níveis se tornam.
- A versão StockSharp funciona de forma autônoma. A atualização automática agora reage aos dias de negociação em vez de cliques em botões, e o gerenciamento de posição acontece por meio de chamadas `BuyMarket`/`SellMarket`/`ClosePosition` padrão.
