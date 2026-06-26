# Estratégia de Vhf Sliding Windows
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Convertida do assessor especialista do MetaTrader 5 **"VHF EA"** de Vladimir Karputov.
- Usa o indicador Vertical Horizontal Filter (VHF) para classificar o regime de mercado como de tendência ou de amplitude.
- Funciona em qualquer instrumento e período suportado pelo StockSharp; basta alterar o parâmetro de tipo de vela para corresponder ao gráfico desejado.

## Lógica de negociação
1. Subscrever a série de velas selecionada e calcular o indicador VHF com período `VhfPeriod` em cada vela concluída.
2. Manter duas janelas deslizantes de valores VHF recentes:
   - **Janela principal (`MainWindowSize`)** – estabelece o intervalo VHF geral e o ponto médio.
   - **Janela de trabalho (`WorkingWindowSize`)** – detecta quebras de curto prazo acima ou abaixo da mediana VHF local.
3. Um regime de tendência altista ou baixista é confirmado apenas quando o valor VHF atual é maior que o ponto médio de ambas as janelas.
4. Enquanto em regime de tendência, comparar o último preço de fechamento com o fechamento há `MainWindowSize` barras:
   - Fechamento mais alto que a referência → o comportamento padrão é abrir/manter uma posição comprada.
   - Fechamento mais baixo que a referência → o comportamento padrão é abrir/manter uma posição vendida.
   - Habilitar `ReverseSignals` para inverter essas direções.
5. A estratégia fecha qualquer posição aberta quando o valor VHF cai de volta para a zona de amplitude (o VHF atual não está acima de ambos os pontos médios).
6. As inversões de posição são tratadas comprando/vendendo volume suficiente para fechar o lado oposto e abrir a nova posição em uma única ordem de mercado.

## Parâmetros
| Parâmetro | Descrição | Padrão | Notas |
|-----------|-------------|---------|-------|
| `MainWindowSize` | Número de valores VHF na janela deslizante primária. | `11` | Deve ser maior que `WorkingWindowSize`. |
| `WorkingWindowSize` | Número de valores VHF na janela secundária. | `7` | Fornece confirmação mais rápida de rompimentos. |
| `VhfPeriod` | Período de retrovisão do Vertical Horizontal Filter. | `9` | Determina a sensibilidade do indicador. |
| `Volume` | Volume de ordem (lotes) usado para novas entradas. | `1` | Adicionado ao valor absoluto da posição atual ao inverter direção. |
| `ReverseSignals` | Inverter a lógica comprado/vendido derivada da direção do preço. | `true` | Corresponde ao comportamento padrão do EA original. |
| `CandleType` | Período e tipo de vela para subscrição de dados. | `Período de 15 minutos` | Alterar para adaptar a estratégia a outros gráficos. |

## Gestão de dinheiro e saídas
- A estratégia sempre negocia um volume fixo definido por `Volume`.
- O gerenciamento de stop protetor é delegado ao auxiliar integrado `StartProtection()` do StockSharp, que fecha com segurança posições residuais inesperadas.
- Nenhum alvo de stop-loss ou take-profit é codificado; as saídas dependem da mudança de regime detectada pelo VHF.

## Notas de implementação
- Usa a API de subscrição de velas de alto nível com vinculação de indicadores, seguindo as diretrizes do projeto.
- Um indicador personalizado Vertical Horizontal Filter idêntico à versão MQL está embutido na estratégia.
- As instruções de log descrevem cada mudança de posição e transição de regime para depuração mais fácil.
