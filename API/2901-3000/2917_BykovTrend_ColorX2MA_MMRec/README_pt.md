# Estratégia BykovTrend + ColorX2MA MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia StockSharp reproduz o especialista MQL5 `Exp_BykovTrend_ColorX2MA_MMRec`. Combina dois módulos independentes:
BykovTrend, que colore velas com um filtro Williams %R, e ColorX2MA, que inspeciona a inclinação de uma média móvel de duplo suavizado.
As entradas são emitidas sempre que o módulo selecionado detecta uma nova mudança de cor/inclinação e o gerenciamento de dinheiro é simplificado
para usar o volume da estratégia. Stop-loss e take-profit percentuais opcionais podem ser habilitados através do bloco de proteção integrado do StockSharp.

## Lógica da estratégia

### Módulo BykovTrend
- Usa um Williams %R (`BykovTrendWprLength`) calculado sobre `BykovTrendCandleType` (padrão velas de 2 horas).
- `BykovTrendRisk` controla os limites altista/baixista (`33 - Risk` e `-Risk`).
- A cor do indicador é avaliada na barra `BykovTrendSignalBar` (deslocamento da barra fechada mais recente).
- Uma cor altista (< 2) fecha vendidos se `AllowBykovTrendCloseSell` estiver habilitado e pode abrir comprados se
  `EnableBykovTrendBuy` for verdadeiro e a cor anterior não era altista.
- Uma cor baixista (> 2) fecha comprados se `AllowBykovTrendCloseBuy` estiver habilitado e pode abrir vendidos se
  `EnableBykovTrendSell` for verdadeiro e a cor anterior não era baixista.

### Módulo ColorX2MA
- Duas etapas de suavização (`ColorX2MaMethod1`, `ColorX2MaLength1` e `ColorX2MaMethod2`, `ColorX2MaLength2`) são aplicadas sobre
  o preço definido por `ColorX2MaPriceType` usando velas de `ColorX2MaCandleType`.
- A saída da segunda etapa é comparada com o valor anterior para gerar estados de inclinação: subindo (1), descendo (2) ou plano (0).
- O estado de inclinação é avaliado na barra `ColorX2MaSignalBar` (deslocamento da última barra fechada).
- Uma inclinação ascendente fecha vendidos (`AllowColorX2MaCloseSell`) e pode abrir comprados (`EnableColorX2MaBuy`) se a inclinação anterior
  ainda não estava subindo.
- Uma inclinação descendente fecha comprados (`AllowColorX2MaCloseBuy`) e pode abrir vendidos (`EnableColorX2MaSell`) se a inclinação anterior
  ainda não estava descendo.

### Gerenciamento de operações
- Os sinais de fechamento são executados antes das aberturas para emular a sequência de ordens do especialista original.
- As ordens usam `Strategy.Volume` como tamanho de posição; o complexo recontador de gerenciamento de dinheiro da versão MQL não
  é replicado.
- `StopLossPercent` e `TakeProfitPercent` ativam `StartProtection` com saídas baseadas em porcentagem quando maiores que zero.

## Detalhes

- **Comprado/Vendido**: Ambas as direções suportadas.
- **Critérios de entrada**:
  - Transição de cor altista do BykovTrend.
  - Transição de inclinação ascendente do ColorX2MA.
- **Critérios de saída**:
  - Cor/inclinação oposta dependendo dos módulos habilitados.
  - Stop-loss/take-profit percentual opcional.
- **Filtros**: Nenhum além da lógica do indicador.
- **Dimensionamento de posição**: Fixo via `Strategy.Volume`.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `EnableBykovTrendBuy` | Permitir que o BykovTrend abra operações compradas. | `true` |
| `EnableBykovTrendSell` | Permitir que o BykovTrend abra operações vendidas. | `true` |
| `AllowBykovTrendCloseBuy` | Fechar comprados quando o BykovTrend se torna baixista. | `true` |
| `AllowBykovTrendCloseSell` | Fechar vendidos quando o BykovTrend se torna altista. | `true` |
| `BykovTrendRisk` | Sensibilidade do Williams %R (valores menores reagem mais rápido). | `3` |
| `BykovTrendWprLength` | Período do Williams %R. | `9` |
| `BykovTrendSignalBar` | Índice de barra (deslocamento) para avaliar a cor do BykovTrend. | `1` |
| `BykovTrendCandleType` | Tipo/período de vela para BykovTrend. | `2h` |
| `EnableColorX2MaBuy` | Permitir que o ColorX2MA abra operações compradas. | `true` |
| `EnableColorX2MaSell` | Permitir que o ColorX2MA abra operações vendidas. | `true` |
| `AllowColorX2MaCloseBuy` | Fechar comprados quando a inclinação do ColorX2MA se torna baixista. | `true` |
| `AllowColorX2MaCloseSell` | Fechar vendidos quando a inclinação do ColorX2MA se torna altista. | `true` |
| `ColorX2MaMethod1` | Tipo de média móvel para a etapa 1. | `Simple` |
| `ColorX2MaLength1` | Período para suavização da etapa 1. | `12` |
| `ColorX2MaPhase1` | Marcador de posição de fase mantido para documentação (não usado). | `15` |
| `ColorX2MaMethod2` | Tipo de média móvel para a etapa 2. | `Jurik` |
| `ColorX2MaLength2` | Período para suavização da etapa 2. | `5` |
| `ColorX2MaPhase2` | Marcador de posição de fase mantido para documentação (não usado). | `15` |
| `ColorX2MaPriceType` | Fonte de preço para suavização do ColorX2MA. | `Close` |
| `ColorX2MaSignalBar` | Índice de barra (deslocamento) para avaliar o estado de inclinação. | `1` |
| `ColorX2MaCandleType` | Tipo/período de vela para ColorX2MA. | `2h` |
| `StopLossPercent` | Stop protetor opcional em porcentagem (0 desabilita). | `0` |
| `TakeProfitPercent` | Take-profit protetor opcional em porcentagem (0 desabilita). | `0` |

## Notas

- `ColorX2MaPhase1` e `ColorX2MaPhase2` são mantidos para refletir os inputs originais, mas não são consumidos porque as
  implementações de médias móveis do StockSharp não expõem um parâmetro de fase.
- Apenas os métodos de suavização disponíveis no StockSharp são fornecidos; as opções de SmoothAlgorithms não suportadas voltam para
  o análogo mais próximo.
- Os recontadores de gerenciamento de dinheiro de `TradeAlgorithms.mqh` não estão portados; o dimensionamento de posição deve ser tratado por controles
  de risco externos ou lógica personalizada no StockSharp.

## Uso

1. Atribuir o instrumento desejado e definir `Strategy.Volume` para o tamanho de lote que se deseja negociar.
2. Configurar os tipos de vela para BykovTrend e ColorX2MA se o período padrão de 2 horas não for apropriado.
3. Ajustar métodos/comprimentos de suavização e deslocamentos de barra de sinal para corresponder à configuração original ou aos próprios testes.
4. Opcionalmente habilitar o bloco de proteção definindo `StopLossPercent` e/ou `TakeProfitPercent` maior que zero.
5. Iniciar a estratégia; ela assinará os fluxos de velas configurados, monitorará ambos os módulos e emitirá ordens de mercado na
   sequência definida acima.
