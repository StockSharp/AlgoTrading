# Estratégia de Spreader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Spreader** é uma abordagem de pair trading inspirada no consultor especialista original do MetaTrader "Spreader". A estratégia monitora dois instrumentos positivamente correlacionados e busca lucrar com divergências de curto prazo mantendo um perfil neutro ao mercado. Quando a posição combinada atinge o alvo monetário desejado, a estratégia fecha ambas as pernas e aguarda a próxima configuração.

O algoritmo é projetado para velas de um minuto por padrão, espelhando o comportamento do EA original, mas o período pode ser ajustado quando a estratégia é carregada no Designer, Shell ou no executor da API.

## Lógica de trading

1. **Preparação de dados**
   - Assina velas para o instrumento primário (o atribuído à estratégia) e o instrumento secundário.
   - Armazena os últimos `2 * ShiftLength + 1` valores de fechamento para cada instrumento. O comprimento de deslocamento padrão é 30 barras.
   - Reage apenas a velas concluídas e requer que ambos os instrumentos produzam uma barra com o mesmo horário de abertura.

2. **Filtro de tendência**
   - Calcula as variações de preço entre o fechamento atual e o fechamento `ShiftLength` barras atrás, bem como a variação entre as amostras do meio e as mais antigas para ambos os instrumentos.
   - Se as duas variações de qualquer instrumento tiverem o mesmo sinal, a estratégia interpreta como uma tendência persistente e ignora a operação.

3. **Verificação de correlação**
   - Garante que o sinal da última variação em ambos os instrumentos seja idêntico. Se o sinal diferir, a correlação é considerada negativa e nenhum spread é aberto.

4. **Alinhamento de volatilidade**
   - Calcula a magnitude absoluta das oscilações recentes (`a` para a perna principal, `b` para a secundária) e usa sua razão para escalar o volume de hedge. Razões fora do intervalo `[0.3, 3]` são rejeitadas por indicarem comportamento instável.

5. **Entrada**
   - Escolhe a direção da perna principal comparando as oscilações normalizadas: se o movimento principal for mais forte, a estratégia compra o instrumento principal e vende a perna secundária; caso contrário, vende a perna principal e compra a secundária.
   - As ordens são enviadas com execução a mercado e os volumes são normalizados para respeitar o passo de lote e os limites mínimo e máximo de cada instrumento.

6. **Gestão de posições**
   - Se apenas a perna secundária estiver aberta (por exemplo, devido a problemas de conectividade), a estratégia abre a perna principal ausente na direção oposta para restaurar o hedge.
   - Se apenas a perna principal permanecer, ela é fechada imediatamente para evitar exposição direcional.
   - Quando ambas as pernas estão presentes, a estratégia monitora o lucro flutuante combinado e fecha ambas as posições quando o alvo monetário configurado é atingido.

7. **Verificações de segurança**
   - O trading é desativado quando o tamanho do contrato (multiplicador) dos dois instrumentos difere, pois o EA original assume especificações contratuais iguais.
   - Todas as solicitações de trading são ignoradas até que a estratégia esteja conectada, sincronizada e autorizada a operar pelo ambiente de hospedagem (`IsFormedAndOnlineAndAllowTrading`).

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `SecondSecurity` | Instrumento que forma a perna de hedge do spread. Este parâmetro é obrigatório. |
| `PrimaryVolume` | Volume de ordem base para o instrumento principal. O volume secundário é escalado usando a razão de oscilação. |
| `TargetProfit` | Lucro absoluto, expresso na moeda da conta, após o qual ambas as pernas são fechadas. |
| `ShiftLength` | Número de barras usadas na comparação das oscilações recentes. A estratégia usa `2 * ShiftLength + 1` velas de cada instrumento. |
| `CandleType` | Série de velas usada para análise. Padrão de período de 1 minuto. |

## Dicas

- A estratégia funciona melhor em instrumentos com correlação positiva estável e perfis de volatilidade semelhantes (por exemplo, pares de moedas altamente correlacionados ou futuros sobre índices).
- As especificações do contrato devem estar alinhadas (tamanho do tick, passo de lote, margem); caso contrário, a normalização de volume pode reduzir significativamente o tamanho das ordens.
- Como a estratégia depende de dados de velas, certifique-se de que ambos os instrumentos recebam atualizações de barra sincronizadas do provedor de dados.

## Requisitos

- Dois instrumentos líquidos com correlação positiva.
- Acesso a dados de mercado e permissões de trading para ambos os instrumentos através dos conectores StockSharp.
- Carteira atribuída à instância da estratégia.
